using System.Collections.Generic;
using System.Linq;
using AF.Core.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using WrongLine.Road.Editor;
using Object = UnityEngine.Object;

namespace WrongLine.Road.Tests
{
    public class RoadCatalogValidatorTests
    {
        private const string LOCATION = "test";

        private readonly List<Object> _created = new List<Object>();
        private ValidationReport _report;

        [SetUp]
        public void SetUpReport()
        {
            _report = new ValidationReport();
        }

        [TearDown]
        public void DestroyCreated()
        {
            foreach (Object created in _created)
            {
                if (created != null)
                {
                    Object.DestroyImmediate(created);
                }
            }

            _created.Clear();
        }

        // --- One piece ---

        [TestCaseSource(nameof(AllDefaultSpecs))]
        public void EveryDefaultPiece_IsHealthy(string name)
        {
            RoadPieceSpecs.Spec spec = RoadPieceSpecs.Defaults.First(candidate => candidate.Name == name);
            RoadPieceView view = MakePiece(spec.Name, spec.Recipe, spec.IsStart);

            RoadCatalogValidator.CheckPiece(view, LOCATION, _report);

            AssertNoIssues();
        }

        [Test]
        public void PieceWithoutARecipe_IsAnError()
        {
            var root = Track(new GameObject("NoRecipe"));
            var view = root.AddComponent<RoadPieceView>();

            RoadCatalogValidator.CheckPiece(view, LOCATION, _report);

            AssertIssue("no recipe");
        }

        [Test]
        public void MeshBuiltFromAnOlderRecipe_IsAnError()
        {
            RoadPieceView view = MakePiece("Stale", Curve(), false);
            var serialized = new SerializedObject(view);
            serialized.FindProperty("_bakedHash").intValue ^= 1;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            RoadCatalogValidator.CheckPiece(view, LOCATION, _report);

            AssertIssue("older version of the recipe");
        }

        [Test]
        public void PieceWithoutAMesh_IsAnError()
        {
            RoadPieceView view = MakePiece("NoMesh", Curve(), false);
            view.GetComponent<MeshFilter>().sharedMesh = null;

            RoadCatalogValidator.CheckPiece(view, LOCATION, _report);

            AssertIssue("no mesh");
        }

        [Test]
        public void WrongMaterialCount_AndEmptySlots_AreErrors()
        {
            RoadPieceView view = MakePiece("Materials", Curve(), false);
            var renderer = view.GetComponent<MeshRenderer>();
            Material[] materials = renderer.sharedMaterials;

            renderer.sharedMaterials = materials.Take(3).ToArray();
            RoadCatalogValidator.CheckPiece(view, LOCATION, _report);
            AssertIssue("3 materials for 4 submeshes");

            _report = new ValidationReport();
            materials[2] = null;
            renderer.sharedMaterials = materials;
            RoadCatalogValidator.CheckPiece(view, LOCATION, _report);
            AssertIssue("material slot 2 is empty");
        }

        [Test]
        public void MeshOfAnotherPiece_DoesNotSitOnTheCentreLine()
        {
            RoadPieceView view = MakePiece("Mismatch", Curve(), false);
            TrackShape other = TrackIntegrator.Bake(new TrackRecipe(60f, -45f, YawProfile.Single, 0f, PitchProfile.Bump, 0f));
            Mesh wrong = Track(RoadMeshBuilder.Build(other, 2));
            view.GetComponent<MeshFilter>().sharedMesh = wrong;

            RoadCatalogValidator.CheckPiece(view, LOCATION, _report);

            AssertIssue("does not sit on the recipe's centre line");
        }

        // --- One centre line ---

        [Test]
        public void BakedShapes_PassTheShapeChecks()
        {
            RoadCatalogValidator.CheckShape(TrackIntegrator.Bake(Curve()), LOCATION, _report, null);

            AssertNoIssues();
        }

        [Test]
        public void EntryOffThePivot_IsAnError()
        {
            TrackShape shape = Reshape(TrackIntegrator.Bake(TrackRecipe.Straight(10f)), (i, frame) => new TrackFrame(frame.Position + Vector3.right, frame.Rotation));

            RoadCatalogValidator.CheckShape(shape, LOCATION, _report, null);

            AssertIssue("not the piece pivot");
        }

        [Test]
        public void BankedEnd_IsAnError()
        {
            TrackShape baked = TrackIntegrator.Bake(TrackRecipe.Straight(10f));
            TrackShape shape = Reshape(baked, (i, frame) => i == baked.Count - 1 ? new TrackFrame(frame.Position, frame.Rotation * Quaternion.AngleAxis(10f, Vector3.forward)) : frame);

            RoadCatalogValidator.CheckShape(shape, LOCATION, _report, null);

            AssertIssue("exit is banked");
        }

        [Test]
        public void SlopedEnd_IsAnError()
        {
            TrackShape baked = TrackIntegrator.Bake(TrackRecipe.Straight(10f));
            TrackShape shape = Reshape(baked, (i, frame) => i == baked.Count - 1 ? new TrackFrame(frame.Position, Quaternion.Euler(-10f, 0f, 0f)) : frame);

            RoadCatalogValidator.CheckShape(shape, LOCATION, _report, null);

            AssertIssue("exit is on a slope");
        }

        [Test]
        public void KinkInTheMiddle_IsABendTighterThanTheMinimum()
        {
            TrackShape baked = TrackIntegrator.Bake(TrackRecipe.Straight(10f));
            TrackShape shape = Reshape(baked, (i, frame) => i == 10 ? new TrackFrame(frame.Position, Quaternion.Euler(0f, 20f, 0f)) : frame);

            RoadCatalogValidator.CheckShape(shape, LOCATION, _report, null);

            AssertIssue("tighter than");
        }

        [Test]
        public void TooTightABend_IsAnError()
        {
            TrackShape shape = TrackIntegrator.Bake(new TrackRecipe(10f, 170f, YawProfile.Single, 0f, PitchProfile.Bump, 0f));

            RoadCatalogValidator.CheckShape(shape, LOCATION, _report, null);

            AssertIssue("tighter than");
        }

        [Test]
        public void AbruptBanking_IsAnError()
        {
            TrackShape baked = TrackIntegrator.Bake(TrackRecipe.Straight(10f));
            TrackShape shape = Reshape(baked, (i, frame) => i == 10 ? new TrackFrame(frame.Position, Quaternion.AngleAxis(10f, Vector3.forward)) : frame);

            RoadCatalogValidator.CheckShape(shape, LOCATION, _report, null);

            AssertIssue("banking changes too abruptly");
        }

        // --- The catalog ---

        [Test]
        public void TheDefaultSet_IsAHealthyCatalog()
        {
            (RoadPieceView start, List<RoadPieceView> pieces) = MakeDefaultCatalog();

            RoadCatalogValidator.CheckCatalog(start, pieces, LOCATION, _report);

            AssertNoIssues();
        }

        [Test]
        public void NoStart_IsAnError()
        {
            (_, List<RoadPieceView> pieces) = MakeDefaultCatalog();

            RoadCatalogValidator.CheckCatalog(null, pieces, LOCATION, _report);

            AssertIssue("no start piece");
        }

        [Test]
        public void StartNotMarkedAsAStart_IsAnError()
        {
            (_, List<RoadPieceView> pieces) = MakeDefaultCatalog();

            RoadCatalogValidator.CheckCatalog(pieces.First(piece => piece.name == "Straight_M"), pieces, LOCATION, _report);

            AssertIssue("not marked as a start");
        }

        [Test]
        public void StartThatIsNotStraight_IsAnError()
        {
            (_, List<RoadPieceView> pieces) = MakeDefaultCatalog();
            RoadPieceView bent = MakePiece("BentStart", Curve(), true);

            RoadCatalogValidator.CheckCatalog(bent, pieces, LOCATION, _report);

            AssertIssue("must be a plain straight");
        }

        [Test]
        public void EmptySelectablePieces_IsAnError()
        {
            (RoadPieceView start, _) = MakeDefaultCatalog();

            RoadCatalogValidator.CheckCatalog(start, new List<RoadPieceView>(), LOCATION, _report);

            AssertIssue("no selectable pieces");
        }

        [Test]
        public void EmptySlot_AndDuplicate_AreErrors()
        {
            (RoadPieceView start, List<RoadPieceView> pieces) = MakeDefaultCatalog();
            pieces.Add(null);
            pieces.Add(pieces[0]);

            RoadCatalogValidator.CheckCatalog(start, pieces, LOCATION, _report);

            AssertIssue("slot 21 is empty");
            AssertIssue("is listed twice");
        }

        [Test]
        public void StartInTheSelectablePieces_IsAnError()
        {
            (RoadPieceView start, List<RoadPieceView> pieces) = MakeDefaultCatalog();
            pieces.Add(start);

            RoadCatalogValidator.CheckCatalog(start, pieces, LOCATION, _report);

            AssertIssue("would pick it mid-road");
        }

        [Test]
        public void MissingTag_IsAnError()
        {
            (RoadPieceView start, List<RoadPieceView> pieces) = MakeDefaultCatalog();
            pieces.RemoveAll(piece => piece.Recipe.Tags.HasFlag(RoadTags.Climb));

            RoadCatalogValidator.CheckCatalog(start, pieces, LOCATION, _report);

            AssertIssue("tagged Climb");
        }

        [Test]
        public void CurvesThatOnlyTurnOneWay_AreAnError()
        {
            (RoadPieceView start, List<RoadPieceView> pieces) = MakeDefaultCatalog();
            pieces.RemoveAll(piece => piece.Recipe.Tags.HasFlag(RoadTags.Curve) && piece.Recipe.Recipe.YawDegrees < 0f);

            RoadCatalogValidator.CheckCatalog(start, pieces, LOCATION, _report);

            AssertIssue("only turn right");
        }

        // --- Helpers ---

        private static IEnumerable<string> AllDefaultSpecs()
        {
            return RoadPieceSpecs.Defaults.Select(spec => spec.Name);
        }

        private static TrackRecipe Curve()
        {
            return new TrackRecipe(60f, 45f, YawProfile.Single, 0f, PitchProfile.Bump, 12f);
        }

        private (RoadPieceView Start, List<RoadPieceView> Pieces) MakeDefaultCatalog()
        {
            RoadPieceView start = null;
            var pieces = new List<RoadPieceView>();
            foreach (RoadPieceSpecs.Spec spec in RoadPieceSpecs.Defaults)
            {
                RoadPieceView view = MakePiece(spec.Name, spec.Recipe, spec.IsStart);
                if (spec.IsStart)
                {
                    start = view;
                }
                else
                {
                    pieces.Add(view);
                }
            }

            return (start, pieces);
        }

        // A piece the way the builder makes it, without the assets: recipe, mesh, four materials and the view.
        private RoadPieceView MakePiece(string name, TrackRecipe recipe, bool isStart)
        {
            var asset = Track(ScriptableObject.CreateInstance<RoadPieceRecipe>());
            asset.name = name;
            var recipeObject = new SerializedObject(asset);
            recipeObject.FindProperty("_length").floatValue = recipe.Length;
            recipeObject.FindProperty("_yawDegrees").floatValue = recipe.YawDegrees;
            recipeObject.FindProperty("_yawProfile").enumValueIndex = (int)recipe.YawProfile;
            recipeObject.FindProperty("_pitchDegrees").floatValue = recipe.PitchDegrees;
            recipeObject.FindProperty("_pitchProfile").enumValueIndex = (int)recipe.PitchProfile;
            recipeObject.FindProperty("_bankDegrees").floatValue = recipe.BankDegrees;
            recipeObject.FindProperty("_isStart").boolValue = isStart;
            recipeObject.ApplyModifiedPropertiesWithoutUndo();

            var root = Track(new GameObject(name));
            TrackShape shape = asset.Shape;
            bool straight = (asset.Tags & ~RoadTags.Straight) == RoadTags.None;
            root.AddComponent<MeshFilter>().sharedMesh = Track(RoadMeshBuilder.Build(shape, straight ? shape.Count - 1 : 2));
            var renderer = root.AddComponent<MeshRenderer>();
            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Hidden/InternalErrorShader");
            renderer.sharedMaterials = Enumerable.Range(0, RoadMeshBuilder.SubmeshCount).Select(_ => Track(new Material(shader))).ToArray();

            var view = root.AddComponent<RoadPieceView>();
            var viewObject = new SerializedObject(view);
            viewObject.FindProperty("_recipe").objectReferenceValue = asset;
            viewObject.FindProperty("_bakedHash").intValue = recipe.StableHash();
            viewObject.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }

        private static TrackShape Reshape(TrackShape source, System.Func<int, TrackFrame, TrackFrame> change)
        {
            var frames = new TrackFrame[source.Count];
            for (int i = 0; i < frames.Length; i++)
            {
                frames[i] = change(i, source[i]);
            }

            return new TrackShape(frames, source.Step);
        }

        private T Track<T>(T created) where T : Object
        {
            _created.Add(created);
            return created;
        }

        private void AssertNoIssues()
        {
            Assert.AreEqual(0, _report.Issues.Count, _report.ToString());
        }

        private void AssertIssue(string fragment)
        {
            Assert.IsTrue(_report.Issues.Any(issue => issue.Severity == ValidationSeverity.Error && issue.Message.Contains(fragment)),
                $"Expected an error containing '{fragment}'. Report: {_report}");
        }
    }
}
