using System;
using System.Collections.Generic;

namespace PDFSearcher.Model
{
    public class Document
    {
        public string File { get; set; }
        public int Matches { get; set; }
        public List<Line> Lines { get; set; } = new List<Line>();

        public Document() { }
    }
}
