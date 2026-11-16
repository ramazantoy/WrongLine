using UnityEngine;

namespace WrongLine.Road
{
    /// <summary>
    /// The road pieces' persistent pool. It lives in the root scope, so the instances survive every scene change: they are
    /// made once (while the menu is up) and only ever switched on and off afterwards.
    /// </summary>
    public interface IRoadPiecePool
    {
        /// <summary>The piece set: ids, shapes and the generator's input.</summary>
        RoadPieceSet Set { get; }

        /// <summary>Pieces spawned and not yet released.</summary>
        int CountActive { get; }

        /// <summary>Makes the waiting instances. Safe to call again; only the first call does anything.</summary>
        void Prewarm();

        /// <summary>Brings in the piece with the given id, placed at the given scene position and rotation, as road piece <paramref name="placedIndex"/>.</summary>
        RoadPieceView Spawn(int placedIndex, RoadPieceInfo piece, Vector3 position, Quaternion rotation);

        /// <summary>Takes a piece back. It is switched off and reset here, not by the caller. Throws for a piece that is not out.</summary>
        void Release(RoadPieceView view);

        void ReleaseAll();
    }
}
