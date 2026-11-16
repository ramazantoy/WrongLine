using System;
using UnityEngine;

namespace WrongLine.Road
{
    /// <summary>
    /// A baked road piece: centre-line frames at a fixed step along the piece, in the piece's local space. The first frame is
    /// the entry (the piece's pivot), the last is the exit that the next piece's entry sits on.
    /// </summary>
    public sealed class TrackShape
    {
        private readonly TrackFrame[] _frames;

        public TrackShape(TrackFrame[] frames, float step)
        {
            if (frames == null || frames.Length < 2)
            {
                throw new ArgumentException("A track needs at least two frames.", nameof(frames));
            }

            if (step <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(step));
            }

            _frames = frames;
            Step = step;
        }

        /// <summary>Distance along the piece between two consecutive frames, in metres.</summary>
        public float Step { get; }

        public int Count => _frames.Length;

        /// <summary>Length of the piece along its centre line, in metres.</summary>
        public float Length => (_frames.Length - 1) * Step;

        public TrackFrame Entry => _frames[0];

        public TrackFrame Exit => _frames[_frames.Length - 1];

        public TrackFrame this[int index] => _frames[index];

        /// <summary>The frame at local distance <paramref name="s"/>, clamped to the piece.</summary>
        public TrackFrame Evaluate(float s)
        {
            if (s <= 0f)
            {
                return _frames[0];
            }

            float position = s / Step;
            int index = (int)position;
            if (index >= _frames.Length - 1)
            {
                return _frames[_frames.Length - 1];
            }

            return TrackFrame.Lerp(_frames[index], _frames[index + 1], position - index);
        }
    }
}
