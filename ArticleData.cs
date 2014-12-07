using System;
using System.Collections.Generic;
using System.Linq;

namespace Staticar
{
    internal class ArticleData
    {
        public string Path { get; set; }

        public DateTime Created { get; set; }

        public string[] Lines { get; set; }

        public string Slug { get; set; }

        public string Content { get; set; }

        public string Title { get; set; }
    }
}