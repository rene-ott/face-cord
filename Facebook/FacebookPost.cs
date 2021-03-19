using System;
using System.Collections.Generic;

namespace FaceCord.Facebook
{
    public class FacebookPost
    {
        public IList<string> Paragraphs { get; set; }
        public string Link { get; set; }

        public string GetText() => string.Join(Environment.NewLine, Paragraphs) + (!string.IsNullOrEmpty(Link) ? $"{Environment.NewLine}{Link}" : string.Empty);

        public bool IsSameWith(FacebookPost fbPost)
        {
            return string.Join("", Paragraphs) == string.Join("", fbPost.Paragraphs);
        }
    }
}
