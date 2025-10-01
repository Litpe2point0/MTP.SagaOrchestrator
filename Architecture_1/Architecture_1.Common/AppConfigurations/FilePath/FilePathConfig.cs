using Microsoft.Extensions.Configuration;
using Architecture_1.Common.AppConfigurations.FilePath.interfaces;

namespace Architecture_1.Common.AppConfigurations.FilePath
{
    public class FilePathConfigModel
    {
        public string ACCOUNt_IMAGE_PATH { get; set; }
        public string FACILITY_IMAGE_PATH { get; set; }
        public string MAJOR_IMAGE_PATH { get; set; }
        public string ITEM_IMAGE_PATH  { get; set; }
        public string SERVICE_IMAGE_PATH { get; set; }
    }
    public class FilePathConfig : IFilePathConfig
    {
        public string ACCOUNt_IMAGE_PATH { get; set; }
        public string FACILITY_IMAGE_PATH { get; set; }
        public string MAJOR_IMAGE_PATH { get; set; }
        public string ITEM_IMAGE_PATH  { get; set; }
        public string SERVICE_IMAGE_PATH { get; set; }

        public FilePathConfig(IConfiguration configuration)
        {

            // ACCOUNt_IMAGE_PATH = configuration["FilePaths:ACCOUNt_IMAGE_PATH"];
            // FACILITY_IMAGE_PATH = configuration["FilePaths:FACILITY_IMAGE_PATH"];
            // MAJOR_IMAGE_PATH = configuration["FilePaths:MAJOR_IMAGE_PATH"];
            // ITEM_IMAGE_PATH = configuration["FilePaths:ITEM_IMAGE_PATH"];
            // SERVICE_IMAGE_PATH = configuration["FilePaths:SERVICE_IMAGE_PATH"];

            var filePaths = configuration.GetSection("FilePaths").Get<FilePathConfigModel>();
            ACCOUNt_IMAGE_PATH = filePaths.ACCOUNt_IMAGE_PATH;
            FACILITY_IMAGE_PATH = filePaths.FACILITY_IMAGE_PATH;
            MAJOR_IMAGE_PATH = filePaths.MAJOR_IMAGE_PATH;
            ITEM_IMAGE_PATH = filePaths.ITEM_IMAGE_PATH;
            SERVICE_IMAGE_PATH = filePaths.SERVICE_IMAGE_PATH;
        }

    }
}
