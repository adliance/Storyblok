using System;
using System.Collections.Generic;

namespace Adliance.Storyblok.Sitemap;

public class SitemapResult
{
    public IList<SitemapLocation> Locations { get; } = new List<SitemapLocation>();

    public class SitemapLocation
    {
        public SitemapLocation(string url, DateTime lastModified)
        {
            Url = url;
            LastModified = lastModified;
        }

        public string Url { get; set; }
        public DateTime LastModified { get; set; }
        public ChangeFrequency ChangeFrequency { get; set; } = ChangeFrequency.Weekly;
        public double Priority { get; set; } = 0.5; // between 0 and 1 (high)
    }

    public enum ChangeFrequency
    {
        // ReSharper disable UnusedMember.Global
        Weekly,
        Yearly,
        Monthly,
        Daily,
        Hourly,
        Always,
        Never
        // ReSharper restore UnusedMember.Global
    }
}
