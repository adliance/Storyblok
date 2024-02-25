using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;

namespace Adliance.Storyblok.Extensions
{
    public static class QueryCollectionExtensions
    {
        public static bool IsInStoryblokEditor(this IQueryCollection query, StoryblokOptions settings)
        {
            if (string.IsNullOrWhiteSpace(query["_storyblok_tk[space_id]"])) // fast check to return false immediately without calculating any hashes
            {
                return false;
            }

            var validationString = $"{query["_storyblok_tk[space_id]"]}:{settings.ApiKeyPreview}:{query["_storyblok_tk[timestamp]"]}";
            using (var sha1 = SHA1.Create())
            {
                var hashBytes = sha1.ComputeHash(Encoding.UTF8.GetBytes(validationString));

                var sb = new StringBuilder();
                foreach (var b in hashBytes)
                {
                    var hex = b.ToString("x2", CultureInfo.InvariantCulture);
                    sb.Append(hex);
                }

                var validationToken = sb.ToString();
                var timestamp = (int)Math.Floor((DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds) - 3600;
                return query["_storyblok_tk[token]"] == validationToken && int.Parse(query["_storyblok_tk[timestamp]"]!, CultureInfo.InvariantCulture) > timestamp;
            }
        }
    }
}
