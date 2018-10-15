namespace Trunked
{
    public enum Classifiers
    {
        Barcode,
        Book,
        Clothing,
        Other
    }

    public class Result
    {
        public Classifiers Classification { get; set; }

        public string Name { get; set; }

        public string Probability { get; set; }
    }
}