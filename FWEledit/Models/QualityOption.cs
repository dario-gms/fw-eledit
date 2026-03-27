namespace FWEledit
{
    public sealed class QualityOption
    {
        public int Value { get; set; }
        public string Label { get; set; }

        public override string ToString()
        {
            return Value.ToString() + " - " + (Label ?? string.Empty);
        }
    }
}
