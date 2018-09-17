namespace Trunked
{
    public enum ResultType
    {
        Barcode,
        Other
    }

    public class Result
    {
        public ResultType Type { get; set; }

        public string Name { get; set; }

        public string Probability { get; set; }
    }
}