using WrongLine.Road;

namespace WrongLine.Driving
{
    /// <summary>The road as the views see it this frame: the chain, how far along it the car is, and where the scene origin sits.</summary>
    internal interface IRoadFrame
    {
        IRoadPath Path { get; }

        /// <summary>Distance along the road at the car, in metres.</summary>
        double Distance { get; }

        /// <summary>Scene positions are world positions minus this.</summary>
        WorldPoint Origin { get; }
    }
}
