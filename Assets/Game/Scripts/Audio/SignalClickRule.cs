namespace WrongLine.Audio
{
    public enum SignalClick
    {
        None,
        Tick,
        Tock,
    }

    /// <summary>The turn signal's relay: a tick when the lamp comes on and a tock when it goes off. Pure.</summary>
    public sealed class SignalClickRule
    {
        private bool _lampOn;

        public SignalClick Observe(bool lampOn)
        {
            if (lampOn == _lampOn)
            {
                return SignalClick.None;
            }

            _lampOn = lampOn;
            return lampOn ? SignalClick.Tick : SignalClick.Tock;
        }
    }
}
