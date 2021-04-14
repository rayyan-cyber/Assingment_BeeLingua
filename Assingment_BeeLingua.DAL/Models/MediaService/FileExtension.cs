using System.Collections.Generic;

namespace Assingment_BeeLingua.DAL.Models.MediaService
{
    public class FileExtension
    {
        public static List<string> VIDEO = new List<string>() {
            "flv",
            "mp4",
            "mpeg",
            "mkv",
            "mov",
        };

        public static List<string> OTHER = new List<string>() {
            // offices
            "docx", "doc",
            "xlsx", "xls",
            "pptx", "ppt",

            // ebooks
            "pdf", "epub",

            // images
            "jpeg", "jpg",
            "png",
        };

    }
}
