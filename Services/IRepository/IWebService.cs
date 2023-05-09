
using TaskManager.API.Data.Models;

namespace TaskManager.API.Services.IRepository
{
    public interface IWebService
    {
        public Task<string> SendEmail(EmailOption emailOption);

        public Task<string> UploadFileToFirebase(FileStream fs, string root, string filename);
        
    }
}