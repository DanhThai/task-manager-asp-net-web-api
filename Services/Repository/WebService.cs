
using Firebase.Auth;
using Firebase.Storage;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using MimeKit.Text;
using TaskManager.API.Data.Models;
using TaskManager.API.Services.IRepository;

namespace TaskManager.API.Services.Repository
{

    public class WebService: IWebService
    {
        private readonly IConfiguration _configuration;
        public WebService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // public async Task<bool> SendEmail(EmailOption emailOption)
        // {
        //     RestClient client = new RestClient(_configuration["Mailgun:BaseUri"]);    
        //     client.Authenticator = new HttpBasicAuthenticator ("api",_configuration["Mailgun:APIKey"]);

        //     RestRequest request = new RestRequest ();
        //     request.AddParameter ("domain", _configuration["Mailgun:Domain"], ParameterType.UrlSegment);
        //     request.Resource = "{domain}/messages";
        //     request.AddParameter ("from", $"{_configuration["Mailgun:SenderName"]} <{_configuration["Mailgun:SenderAddress"]}>");
        //     // request.AddParameter ("to", emailOption.ToEmail);
        //     request.AddParameter ("to", "danhthai5343@gmail.com");
        //     request.AddParameter ("subject", emailOption.Subject);
        //     request.AddParameter ("html", emailOption.Body);
        //     request.Method = Method.Post;

        //     var response = await client.ExecuteAsync(request);

        //     return response.IsSuccessful;
        // }
        public async Task<string> SendEmail(EmailOption emailOption)
        {
            try{
                var email = new MimeMessage();
                email.From.Add(MailboxAddress.Parse(_configuration["MailSettings:From"]));
                email.To.Add(MailboxAddress.Parse(emailOption.ToEmail));
                // email.To.Add(MailboxAddress.Parse("danhthai5343@gmail.com"));

                email.Subject = emailOption.Subject;
                email.Body = new TextPart(TextFormat.Html) {Text = emailOption.Body};

                var client = new SmtpClient();
                await client.ConnectAsync(_configuration["MailSettings:Host"], 587, SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(_configuration["MailSettings:From"], _configuration["MailSettings:Password"]);
                var rs = await client.SendAsync(email);
                await client.DisconnectAsync(true);
                return rs;             
            }
            catch(Exception e){
                return e.Message;
            }
            
        }

        public async Task<string> UploadFileToFirebase(FileStream fs, string root, string filename)
        {
            try{ 
            // authenticate firebase
                var auth = new FirebaseAuthProvider(new FirebaseConfig(_configuration["Firebase:ApiKey"]));
                var signIn = await auth.SignInWithEmailAndPasswordAsync(
                                    _configuration["Firebase:AuthEmail"],
                                    _configuration["Firebase:AuthPassword"]);

                // you can use CancellationTokenSource to cancel the upload midway
                var cancellation = new CancellationTokenSource();

                var imgUpload = new FirebaseStorage(
                    _configuration["Firebase:Bucket"],
                    new FirebaseStorageOptions
                    {
                        AuthTokenAsyncFactory = () => Task.FromResult(signIn.FirebaseToken),
                        // when you cancel the upload, exception is thrown. By default no exception is thrown
                        ThrowOnCancel = true
                    })
                    .Child(root)
                    .Child(filename)
                    .PutAsync(fs, cancellation.Token);
                imgUpload.Progress.ProgressChanged += (s, e) => Console.WriteLine($"Progress: {e.Percentage} %");
                var imgUrl = await imgUpload;
                return imgUrl;
            }
            catch (Exception e){
                throw e;
            }
        }
    }
}