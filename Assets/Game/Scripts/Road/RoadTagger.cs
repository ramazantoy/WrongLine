namespace WrongLine.Road
{
    public static class RoadTagger
    {
        public static RoadTags From(in TrackRecipe recipe)
        {
            RoadTags tags = RoadTags.None;

            if (recipe.YawDegrees != 0f)
            {
                tags |= recipe.YawProfile == YawProfile.S ? RoadTags.SCurve : RoadTags.Curve;
            }

            if (recipe.PitchDegrees != 0f)
            {
                bool up = recipe.PitchDegrees > 0f;
                if (recipe.PitchProfile == PitchProfile.Bump)
                {
                    tags |= up ? RoadTags.Climb : RoadTags.Drop;
                }
                else
                {
                    tags |= up ? RoadTags.Hill : RoadTags.Dip;
                }
            }

            if (recipe.BankDegrees != 0f)
            {
                tags |= RoadTags.Banked;
            }

            return tags == RoadTags.None ? RoadTags.Straight : tags;
        }
    }
}
