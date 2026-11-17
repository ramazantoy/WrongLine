using System.Collections.Generic;
using UnityEngine;

namespace WrongLine.Scenery
{
    /// <summary>The heads of the street lamps that are standing along the road now (their transforms move with the scene origin).</summary>
    internal interface IRoadsideLamps
    {
        IReadOnlyList<Transform> Heads { get; }
    }
}
