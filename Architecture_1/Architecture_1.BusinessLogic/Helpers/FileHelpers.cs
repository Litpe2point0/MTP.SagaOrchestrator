using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Drawing;
using Architecture_1.Common.AppConfigurations.App.interfaces;

namespace Architecture_1.BusinessLogic.Helpers
{
    public class FileHelpers
    {
        // CONFIG
        private readonly IAppConfig _appConfig;
        private readonly string _baseUrl;
        private readonly string _wwwRootPath;

        public FileHelpers(IConfiguration configuration, IWebHostEnvironment env, IAppConfig appConfig)
        {
            _appConfig = appConfig;
            // Lấy BASE_URL từ AppConfig
            _baseUrl = appConfig.APP_BASE_URL;
            // Lấy đường dẫn tuyệt đối đến wwwroot
            _wwwRootPath = env.WebRootPath; // Đường dẫn tuyệt đối đến wwwroot
        }

        public string ResizeBase64Image(string base64String, int maxWidth, int maxHeight)
        {
            // Giải mã Base64 thành byte[]
            byte[] imageBytes = Convert.FromBase64String(base64String);

            using (MemoryStream ms = new MemoryStream(imageBytes))
            using (Bitmap originalBitmap = new Bitmap(ms))
            {
                int newWidth = Math.Min(originalBitmap.Width, maxWidth);
                int newHeight = Math.Min(originalBitmap.Height, maxHeight);

                using (Bitmap resizedBitmap = new Bitmap(originalBitmap, newWidth, newHeight))
                using (MemoryStream outputMs = new MemoryStream())
                {
                    resizedBitmap.Save(outputMs, System.Drawing.Imaging.ImageFormat.Jpeg);
                    return Convert.ToBase64String(outputMs.ToArray());
                }
            }
        }


        public async Task<string> GetImageUrl(string rootFolderPath, string folderName, string fileName)
        {
            try
            {
                fileName = await GetFullFileName(rootFolderPath + "/" + folderName, fileName);
                if (fileName == null)
                {
                    return $"{_baseUrl}/{rootFolderPath}/unknown.jpg";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("\n\n\n" + ex.ToString() + "\n\n\n");
                // return $"{_baseUrl}/{rootFolderPath}/unknown.jpg";
            }
            return $"{_baseUrl}/{rootFolderPath}/{folderName}/{fileName}";
        }

        public async Task SaveBase64File(string base64Data, string folderPath, string fileName)
        {
            try
            {
                var match = Regex.Match(base64Data, "^data:(.+);base64,(.+)$");
                string extension = ".png";
                string data = base64Data;

                if (match.Success)
                {
                    string mimeType = match.Groups[1].Value;
                    data = match.Groups[2].Value;
                    extension = mimeType switch
                    {
                        "image/jpeg" => ".jpg",
                        "image/png" => ".png",
                        "image/gif" => ".gif",
                        _ => ".png"
                    };
                }

                string fullFileName = fileName + extension;
                string filePath = Path.Combine(_wwwRootPath, folderPath, fullFileName);

                // kiểm tra xem folder đã tồn tại chưa
                if (!Directory.Exists(Path.Combine(_wwwRootPath, folderPath)))
                {
                    Directory.CreateDirectory(Path.Combine(_wwwRootPath, folderPath));
                }
                else
                {
                    // xóa file cũ cùng tên
                    string[] files = Directory.GetFiles(Path.Combine(_wwwRootPath, folderPath));
                    foreach (var file in files)
                    {
                        string fileWithoutExt = Path.GetFileNameWithoutExtension(file);
                        if (fileWithoutExt == fileName)
                        {
                            File.Delete(file);
                        }
                    }
                }

                byte[] buffer = Convert.FromBase64String(data);
                await File.WriteAllBytesAsync(filePath, buffer);


            }
            catch (Exception ex)
            {
                Console.WriteLine("\n\n\n" + ex.ToString() + "\n\n\n");
                throw new Exception($"Error saving file {fileName}: {ex.Message}");
            }
        }



        public async Task SaveFile(byte[] fileData, string folderName, string fileName, string originalFileName)
        {
            try
            {
                string extension = Path.GetExtension(originalFileName);
                string fullFileName = fileName + extension;
                string filePath = Path.Combine(_wwwRootPath, folderName, fullFileName);

                if (!Directory.Exists(Path.Combine(_wwwRootPath, folderName)))
                {
                    Directory.CreateDirectory(Path.Combine(_wwwRootPath, folderName));
                }
                else
                {
                    // xóa file cũ cùng tên
                    string[] files = Directory.GetFiles(Path.Combine(_wwwRootPath, folderName));
                    foreach (var file in files)
                    {
                        string fileWithoutExt = Path.GetFileNameWithoutExtension(file);
                        if (fileWithoutExt == fileName)
                        {
                            File.Delete(file);
                        }
                    }
                }

                await File.WriteAllBytesAsync(filePath, fileData);

            }
            catch (Exception ex)
            {
                Console.WriteLine("\n\n\n" + ex.ToString() + "\n\n\n");
                throw new Exception($"Error saving file {fileName}: {ex.Message}");
            }
        }

        public async Task DeleteFile(string folderName, string fileName)
        {
            try
            {
                string fileToDelete = await GetFullFileName(folderName, fileName);
                if (fileToDelete == null) return;

                string filePath = Path.Combine(_wwwRootPath, folderName, fileToDelete);

                if (!File.Exists(filePath)) return;

                File.Delete(filePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine("\n\n\n DeleteFile:" + ex.ToString() + "\n\n\n");
                throw new Exception($"Error deleting file {fileName}: {ex.Message}");
            }
        }

        public async Task<string> GetFullFileName(string folderName, string fileName)
        {
            try
            {
                string folderPath = Path.Combine(_wwwRootPath, folderName);
                if (!Directory.Exists(folderPath))
                {
                    return null;
                }

                string[] files = Directory.GetFiles(folderPath);
                foreach (var file in files)
                {
                    string fileWithoutExt = Path.GetFileNameWithoutExtension(file);
                    if (fileWithoutExt == fileName)
                    {
                        return Path.GetFileName(file);
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine("\n\n\n" + ex.ToString() + "\n\n\n");
                throw new Exception($"Error getting file name {fileName}: {ex.Message}");
            }
        }

        public async Task CopyFile(string sourceFolder, string sourceFileName, string destinationFolder, string destinationFileName)
        {
            try
            {
                string fullSourceFileName = await GetFullFileName(sourceFolder, sourceFileName);
                if (fullSourceFileName == null) return;


                string sourceFilePath = Path.Combine(_wwwRootPath, sourceFolder, fullSourceFileName);
                string destinationFilePath = Path.Combine(_wwwRootPath, destinationFolder, destinationFileName + Path.GetExtension(fullSourceFileName));

                if (!Directory.Exists(Path.GetDirectoryName(destinationFilePath)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(destinationFilePath));
                }

                File.Copy(sourceFilePath, destinationFilePath, true);
            }
            catch (Exception ex)
            {
                Console.WriteLine("\n\n\n CopyFile: " + ex.ToString() + "\n\n\n");
                throw new Exception($"Error copying file: {ex.Message}");
            }
        }

        public async Task CreateFolder(string folderName)
        {
            try
            {
                if (Directory.Exists(Path.Combine(_wwwRootPath, folderName)))
                {
                    return;
                }
                Directory.CreateDirectory(Path.Combine(_wwwRootPath, folderName));
            }
            catch (Exception ex)
            {
                throw new Exception($"Error creating folder {folderName}: {ex.Message}");
            }
        }

        public async Task DeleteFolder(string folderName)
        {
            try
            {
                if (!Directory.Exists(Path.Combine(_wwwRootPath, folderName)))
                {
                    return;
                }
                Directory.Delete(Path.Combine(_wwwRootPath, folderName), true);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error deleting folder {folderName}: {ex.Message}");
            }
        }
    }
}
