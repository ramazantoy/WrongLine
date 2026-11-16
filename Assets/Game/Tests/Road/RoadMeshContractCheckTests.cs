using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using WrongLine.Road.Editor;

namespace WrongLine.Road.Tests
{
    /// <summary>
    /// The acceptance test for road meshes from any source (the Blender pieces of M1b): the check must pass a right mesh
    /// however it is built and must name what is wrong with a mesh that breaks the contract.
    /// </summary>
    public class RoadMeshContractCheckTests
    {
        private static readonly TrackRecipe Straight = TrackRecipe.Straight(40f);
        private static readonly TrackRecipe BankedBend = new TrackRecipe(60f, 70f, YawProfile.Single, 10f, PitchProfile.Bump, 15f);

        private RoadTestKit _kit;

        [SetUp]
        public void SetUpKit()
        {
            _kit = new RoadTestKit();
        }

        [TearDown]
        public void TearDownKit()
        {
            _kit.Dispose();
        }

        // --- Right meshes pass, however they are built ---

        [TestCaseSource(nameof(AllDefaultSpecs))]
        public void EveryDefaultPiece_Passes(string name)
        {
            RoadPieceSpecs.Spec spec = RoadPieceSpecs.Defaults.First(candidate => candidate.Name == name);
            TrackShape shape = TrackIntegrator.Bake(spec.Recipe);
            bool straight = RoadTagger.From(spec.Recipe) == RoadTags.Straight;

            Mesh mesh = _kit.Track(RoadMeshBuilder.Build(shape, straight ? shape.Count - 1 : 2));

            CollectionAssert.IsEmpty(RoadMeshContractCheck.Check(mesh, shape), name);
        }

        [TestCase(1)]
        [TestCase(4)]
        [TestCase(10)]
        public void AnyRingSpacing_Passes_OnABankedBend(int stride)
        {
            TrackShape shape = TrackIntegrator.Bake(BankedBend);

            Mesh mesh = _kit.Track(RoadMeshBuilder.Build(shape, stride));

            CollectionAssert.IsEmpty(RoadMeshContractCheck.Check(mesh, shape));
        }

        [Test]
        public void AnyVertexAndTriangleOrder_Passes_WhichTheOldEndCheckCouldNotTell()
        {
            TrackShape shape = TrackIntegrator.Bake(BankedBend);
            Mesh built = _kit.Track(RoadMeshBuilder.Build(shape, 2));

            Mesh shuffled = _kit.Track(Shuffled(built, 12345));

            CollectionAssert.IsEmpty(RoadMeshContractCheck.Check(shuffled, shape));
            Assert.Greater(Math.Max(RoadMeshBuilder.EntryGap(shuffled, shape), RoadMeshBuilder.ExitGap(shuffled, shape)), 0.01f,
                "The check that read the builder's own vertex order would have rejected this right mesh.");
        }

        // --- Wrong meshes are named ---

        [Test]
        public void ShiftedAlongThePiece_IsCaught()
        {
            AssertProblem(Straight, mesh => Modified(mesh, v => v + new Vector3(0f, 0f, 0.1f)), "does not run from the entry to the exit");
        }

        [Test]
        public void TooShort_IsCaught()
        {
            AssertProblem(Straight, mesh => Modified(mesh, v => new Vector3(v.x, v.y, v.z * 0.9f)), "does not run from the entry to the exit");
        }

        [Test]
        public void PokingPastTheExit_IsCaught()
        {
            AssertProblem(Straight, mesh => Modified(mesh, v => v.z > 39.9f ? v + new Vector3(0f, 0f, 0.5f) : v), "reaches past the piece's ends");
        }

        [Test]
        public void TooWide_IsCaught()
        {
            AssertProblem(Straight, mesh => Modified(mesh, v => new Vector3(v.x * 1.1f, v.y, v.z)), "m wide");
        }

        [Test]
        public void SurfaceNotAtHeightZero_IsCaught()
        {
            AssertProblem(Straight, mesh => Modified(mesh, v => v + new Vector3(0f, 0.1f, 0f)), "surface is not at height 0");
        }

        [Test]
        public void MirroredWithoutFlippingTheWinding_IsCaught()
        {
            AssertProblem(Straight, mesh => Modified(mesh, v => new Vector3(-v.x, v.y, v.z)), "face down");
        }

        [Test]
        public void AxesSwappedLikeAForgottenBlenderConversion_IsCaught()
        {
            // (x, z, y) keeps the width but lays the piece's length along the height.
            AssertProblem(Straight, mesh => Modified(mesh, v => new Vector3(v.x, v.z, v.y)), "outside the section's height");
            AssertProblem(Straight, mesh => Modified(mesh, v => new Vector3(v.x, v.z, v.y)), "does not run from the entry to the exit");
        }

        [Test]
        public void UvOutOfRange_IsCaught()
        {
            AssertProblem(Straight, mesh => Modified(mesh, null, uv => new Vector2(uv.x * 2f, uv.y)), "u does not run 0 to 1");
            AssertProblem(Straight, mesh => Modified(mesh, null, uv => new Vector2(uv.x, uv.y * 2f)), "v is not distance along the piece");
        }

        [Test]
        public void NoUvs_IsCaught()
        {
            AssertProblem(Straight, mesh => Modified(mesh, dropUvs: true), "no UVs");
        }

        [Test]
        public void WrongSubmeshCount_IsCaught()
        {
            AssertProblem(Straight, mesh =>
            {
                Mesh merged = Modified(mesh);
                merged.subMeshCount = 3;
                return merged;
            }, "3 submeshes");
        }

        [Test]
        public void EmptyAsphalt_IsCaught()
        {
            AssertProblem(Straight, mesh =>
            {
                Mesh copy = Modified(mesh);
                copy.SetTriangles(new int[0], RoadMeshBuilder.SubmeshAsphalt);
                return copy;
            }, "asphalt submesh (the first) is empty");
        }

        [Test]
        public void CurbAndLineSwapped_IsCaught()
        {
            // The artist's first export had asphalt, line, curb, body: the line material would have been drawn on the curb.
            AssertProblem(Straight, mesh =>
            {
                Mesh copy = Modified(mesh);
                int[] curb = copy.GetIndices(RoadMeshBuilder.SubmeshCurb);
                int[] line = copy.GetIndices(RoadMeshBuilder.SubmeshLine);
                copy.SetTriangles(line, RoadMeshBuilder.SubmeshCurb);
                copy.SetTriangles(curb, RoadMeshBuilder.SubmeshLine);
                return copy;
            }, "submesh 2 is not the curb");
        }

        [Test]
        public void BodyAndCurbSwapped_IsCaught()
        {
            AssertProblem(Straight, mesh =>
            {
                Mesh copy = Modified(mesh);
                int[] curb = copy.GetIndices(RoadMeshBuilder.SubmeshCurb);
                int[] body = copy.GetIndices(RoadMeshBuilder.SubmeshBody);
                copy.SetTriangles(body, RoadMeshBuilder.SubmeshCurb);
                copy.SetTriangles(curb, RoadMeshBuilder.SubmeshBody);
                return copy;
            }, "is not the curb");
        }

        [Test]
        public void EmptyLine_IsCaught()
        {
            AssertProblem(Straight, mesh =>
            {
                Mesh copy = Modified(mesh);
                copy.SetTriangles(new int[0], RoadMeshBuilder.SubmeshLine);
                return copy;
            }, "line submesh (number 3) is empty");
        }

        [Test]
        public void TrianglesWithNoArea_AreCaught()
        {
            AssertProblem(Straight, mesh =>
            {
                Mesh copy = Modified(mesh);
                List<int> asphalt = copy.GetIndices(RoadMeshBuilder.SubmeshAsphalt).ToList();
                asphalt.AddRange(new[] { 0, 0, 0 });
                copy.SetTriangles(asphalt, RoadMeshBuilder.SubmeshAsphalt);
                return copy;
            }, "have no area");
        }

        [Test]
        public void AMeshForAnotherPiece_IsCaught_OnACurvedShape()
        {
            TrackShape shape = TrackIntegrator.Bake(BankedBend);
            TrackShape other = TrackIntegrator.Bake(new TrackRecipe(60f, -70f, YawProfile.Single, 10f, PitchProfile.Bump, -15f));
            Mesh wrong = _kit.Track(RoadMeshBuilder.Build(other, 2));

            Assert.IsNotEmpty(RoadMeshContractCheck.Check(wrong, shape));
        }

        [Test]
        public void TheValidator_AcceptsAShuffledMesh_AndRejectsABrokenOne()
        {
            RoadPieceView view = _kit.MakePrefab("Artist", BankedBend, false);
            TrackShape shape = view.Recipe.Shape;
            Mesh built = _kit.Track(RoadMeshBuilder.Build(shape, 2));
            AttachMesh(view, Shuffled(built, 7));
            var report = new AF.Core.Editor.ValidationReport();

            RoadCatalogValidator.CheckPiece(view, "artist", report);
            Assert.AreEqual(0, report.ErrorCount, report.ToString());

            AttachMesh(view, Modified(built, v => v + new Vector3(0f, 0f, 0.1f)));
            report = new AF.Core.Editor.ValidationReport();
            RoadCatalogValidator.CheckPiece(view, "artist", report);
            Assert.Greater(report.ErrorCount, 0);
            StringAssert.Contains("breaks the road piece contract", report.ToString());
        }

        // --- Helpers ---

        private static IEnumerable<string> AllDefaultSpecs() => RoadPieceSpecs.Defaults.Select(spec => spec.Name);

        private void AssertProblem(TrackRecipe recipe, Func<Mesh, Mesh> break_, string fragment)
        {
            TrackShape shape = TrackIntegrator.Bake(recipe);
            Mesh built = _kit.Track(RoadMeshBuilder.Build(shape, shape.Count - 1));
            Assert.IsEmpty(RoadMeshContractCheck.Check(built, shape), "Precondition: the unbroken mesh is right.");

            Mesh broken = _kit.Track(break_(built));
            List<string> problems = RoadMeshContractCheck.Check(broken, shape);

            Assert.IsTrue(problems.Any(problem => problem.Contains(fragment)), $"Expected a problem containing '{fragment}'. Got: {string.Join(" | ", problems)}");
        }

        private void AttachMesh(RoadPieceView view, Mesh mesh)
        {
            // (A destroyed or missing Unity component compares equal to null but is not a null reference, so no `??` here.)
            if (!view.TryGetComponent(out MeshFilter filter))
            {
                filter = view.gameObject.AddComponent<MeshFilter>();
            }

            filter.sharedMesh = _kit.Track(mesh);
            if (!view.TryGetComponent(out MeshRenderer renderer))
            {
                renderer = view.gameObject.AddComponent<MeshRenderer>();
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Hidden/InternalErrorShader");
            renderer.sharedMaterials = Enumerable.Range(0, RoadMeshBuilder.SubmeshCount).Select(_ => _kit.Track(new Material(shader))).ToArray();
        }

        // A copy with every vertex and UV passed through the given functions, as an exporter might change them.
        private static Mesh Modified(Mesh source, Func<Vector3, Vector3> vertex = null, Func<Vector2, Vector2> uv = null, bool dropUvs = false)
        {
            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var uvs = new List<Vector2>();
            source.GetVertices(vertices);
            source.GetNormals(normals);
            source.GetUVs(0, uvs);

            var copy = new Mesh { indexFormat = source.indexFormat };
            copy.SetVertices(vertex == null ? vertices : vertices.Select(vertex).ToList());
            copy.SetNormals(normals);
            if (!dropUvs)
            {
                copy.SetUVs(0, uv == null ? uvs : uvs.Select(uv).ToList());
            }

            copy.subMeshCount = source.subMeshCount;
            for (int submesh = 0; submesh < source.subMeshCount; submesh++)
            {
                copy.SetTriangles(source.GetIndices(submesh), submesh);
            }

            return copy;
        }

        // The same mesh with its vertices in a random order and its triangles in a random order.
        private static Mesh Shuffled(Mesh source, int seed)
        {
            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var uvs = new List<Vector2>();
            source.GetVertices(vertices);
            source.GetNormals(normals);
            source.GetUVs(0, uvs);

            var random = new System.Random(seed);
            int[] order = Enumerable.Range(0, vertices.Count).OrderBy(_ => random.Next()).ToArray(); // order[new] = old
            var newIndexOfOld = new int[vertices.Count];
            for (int n = 0; n < order.Length; n++)
            {
                newIndexOfOld[order[n]] = n;
            }

            var copy = new Mesh { indexFormat = source.indexFormat };
            copy.SetVertices(order.Select(old => vertices[old]).ToList());
            copy.SetNormals(order.Select(old => normals[old]).ToList());
            copy.SetUVs(0, order.Select(old => uvs[old]).ToList());
            copy.subMeshCount = source.subMeshCount;
            for (int submesh = 0; submesh < source.subMeshCount; submesh++)
            {
                int[] indices = source.GetIndices(submesh);
                var triangles = Enumerable.Range(0, indices.Length / 3)
                    .Select(t => new[] { newIndexOfOld[indices[t * 3]], newIndexOfOld[indices[t * 3 + 1]], newIndexOfOld[indices[t * 3 + 2]] })
                    .OrderBy(_ => random.Next())
                    .SelectMany(t => t)
                    .ToArray();
                copy.SetTriangles(triangles, submesh);
            }

            return copy;
        }
    }
}
