namespace ShuttleMate.ModelViews.StopEstimateModelViews
{
    public class ResponseVietMapRouteApi
    {
        public string License { get; set; }
        public string Code { get; set; }
        public string Messages { get; set; }
        public List<Path> Paths { get; set; }
    }

    public class Path
    {
        public double Distance { get; set; }
        public double Weight { get; set; }
        public int Time { get; set; }
        public int Transfers { get; set; }
        public bool PointsEncoded { get; set; }
        public List<double> Bbox { get; set; }
        public string Points { get; set; }
        public List<Instruction> Instructions { get; set; }
    }

    public class Instruction
    {
        public double Distance { get; set; }
        public int Heading { get; set; }
        public int Sign { get; set; }
        public List<int> Interval { get; set; }
        public string Text { get; set; }
        public int Time { get; set; }
        public string StreetName { get; set; }
        public object LastHeading { get; set; }
    }
}
