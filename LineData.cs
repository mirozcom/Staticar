using System;
using System.Collections.Generic;
using System.Linq;

namespace Staticar
{
    public enum LineType
    {
        OlClose,
        UlClose,
        H1,
        H2,
        H3,
        H4,
        OlOpen,
        Li,
        UlOpen,
        Text,
        ListOver,
        Reference
    }

    internal class LineData
    {
        private LineType lineType;
        private string text;

        public LineData(LineType lineType)
        {
            // TODO: Complete member initialization
            this.lineType = lineType;
        }

        public LineData(LineType lineType, string text)
        {
            // TODO: Complete member initialization
            this.lineType = lineType;
            this.text = text;
        }

        public string Text { get { return this.text; } }

        public LineType Type { get { return this.lineType; } set { this.lineType = value; } }
    }
}