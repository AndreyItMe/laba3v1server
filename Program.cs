using System;
using System.IO;
using System.Net;
using System.Text;
using static System.Collections.Specialized.BitVector32;

class TextFileServer
{
    readonly string FILE_STORAGE_PATH_CONST = Path.GetFullPath("C:\\Users\\andrey\\Desktop\\4sem\\КСиС\\lab3\\ksis\\temp"); 
    string FILE_STORAGE_PATH = Path.GetFullPath("C:\\Users\\andrey\\Desktop\\4sem\\КСиС\\lab3\\ksis\\temp");
    const string SERVER_URL = "http://localhost:8080/";
    //"http://127.0.0.1:8888/connection/"
    //const string SERVER_URL = "http://192.168.1.54:8080/";

    HttpListener listener;

    public TextFileServer(string prefix)
    {
        listener = new HttpListener();
        listener.Prefixes.Add(prefix);
    }

    public void Start()
    {
        listener.Start();
        Console.WriteLine("Server started...");
        Listen();
    }

    public async void Listen()
    {
        while (true)
        {
            try
            {
                HttpListenerContext context = await listener.GetContextAsync();
                ProcessRequest(context);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }
        }
    }
    public static string[] GetAdressOfFiles(string input)
    {
       
        int index = input.IndexOf(".txt");

        if (index != -1) //если нет ".txt", то бб
        {
            
            string target = input.Substring(0, index + 4); 

          
            string destination = input.Substring(index + 4).TrimStart('/', ' ');

            return new string[] { target, destination };
        }
        else 
        {
          
            return new string[] { "", "" };
        }
    }
    public static string[] GetAdressOfDir(string input)
    {
       
        int index = input.IndexOf("|");

        if (index != -1)
        {
            
            string target = input.Substring(0, index);

          
            string destination = input.Substring(index+1).TrimStart('/',' ');

            return new string[] { target, destination };
        }
        else
        {
          
            return new string[] { "", "" };
        }
    }
    public void ProcessRequest(HttpListenerContext context)
    {
        string method = context.Request.HttpMethod;
        string requestedUrl = context.Request.Url.LocalPath.Trim('/');
        string[] urlParts = requestedUrl.Split('/');
        string action = urlParts[0].ToLower();
        //string action = context.Request.HttpMethod;
        string target = urlParts.Length >= 1 ? requestedUrl.Substring(action.Length + 1) : "";
        //string target = requestedUrl;
        //Console.WriteLine(context.ToString());
        //rawurl
        //Console.WriteLine(context.Request.ToString());
        Console.WriteLine(context.Request.RawUrl);
        switch (action)
        {
            case "get":
                ServeFile(context, target);
                break;


            case "PUT": //put
                SaveFile(context, target);
                break;
            case "post":
                AppendToFile(context, target);
                break;
            case "delete":
                DeleteFile(context, target);
                break;
            case "copy":
                if (urlParts.Length < 3)
                {
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    context.Response.Close();
                    return;
                }
                string[] result1 = GetAdressOfFiles(target);
                CopyFile(context, result1[0], result1[1]);
                break;
            case "move":
                if (urlParts.Length < 3)
                {
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    context.Response.Close();
                    return;
                }
                string[] result2 = GetAdressOfFiles(target);
                MoveFile(context, result2[0], result2[1]);
                break;
            case "renamefile":
                if (urlParts.Length < 3)
                {
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    context.Response.Close();
                    return;
                }
                string[] result3 = GetAdressOfFiles(target);
                RenameFile(context, result3[0], result3[1]);
                break;

            case "renamedir":
                if (urlParts.Length < 3)
                {
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    context.Response.Close();
                    return;
                }
                string[] result4 = GetAdressOfDir(target);
                RenameDirectory(context, result4[0], result4[1]);
                break;
            case "mkdir":
                MakeDirectory(context, target);
                break;
            case "chdir":
                ChangeDirectory(context, target);
                break;
            case "parent":
                ChangeDirectory(context, "..");
                break;
            case "deletedir":
                DeleteDirectory(context, target);
                break;
            case "list":
                ListFilesRecursive(context);
                break;
            default:
                context.Response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
                context.Response.Close();
                break;
        }
    }

    public bool IsPathInsideStorage(string path)
    {
        string fullPath = Path.GetFullPath(path);
        string storageFullPath = Path.GetFullPath(FILE_STORAGE_PATH);
        if (fullPath == storageFullPath)
            return false;
        if (FILE_STORAGE_PATH.Length > fullPath.Length)
            return storageFullPath.StartsWith(fullPath); 
        return fullPath.StartsWith(storageFullPath);
    }

    public void ServeFile(HttpListenerContext context, string fileName)
    {
        string filePath = Path.Combine(FILE_STORAGE_PATH, fileName);
        try
        {
            if (File.Exists(filePath))
            {
                string content = File.ReadAllText(filePath);
                byte[] buffer = Encoding.UTF8.GetBytes(content);

                context.Response.StatusCode = (int)HttpStatusCode.OK;
                context.Response.ContentType = "text/plain";
                context.Response.ContentLength64 = buffer.Length;
                context.Response.OutputStream.Write(buffer, 0, buffer.Length);
            }
            else
            {
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
            }
        }
        catch (Exception ex)
        {
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            Console.WriteLine("Error: " + ex.Message);
        }
        context.Response.Close();
    }

    public void SaveFile(HttpListenerContext context, string fileName)
    {
        string filePath = Path.Combine(FILE_STORAGE_PATH, fileName);
        try
        {
            using (StreamReader reader = new StreamReader(context.Request.InputStream))
            {
                string content = reader.ReadToEnd();
                File.WriteAllText(filePath, content);
            }
            context.Response.StatusCode = (int)HttpStatusCode.OK;
        }
        catch (Exception ex)
        {
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            Console.WriteLine("Error: " + ex.Message);
        }
        context.Response.Close();
    }

    public void AppendToFile(HttpListenerContext context, string fileName)
    {
        string filePath = Path.Combine(FILE_STORAGE_PATH, fileName); //!!!
        try
        {
            using (StreamWriter writer = File.AppendText(filePath))
            {
                using (StreamReader reader = new StreamReader(context.Request.InputStream))
                {
                    string content = reader.ReadToEnd();
                    writer.Write(content);
                }
            }
            context.Response.StatusCode = (int)HttpStatusCode.OK;
        }
        catch (Exception ex)
        {
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            Console.WriteLine("Error: " + ex.Message);
        }
        context.Response.Close();
    }

    public void DeleteFile(HttpListenerContext context, string fileName)
    {
        string filePath = Path.Combine(FILE_STORAGE_PATH, fileName);
        try
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                context.Response.StatusCode = (int)HttpStatusCode.OK;
            }
            else
            {
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
            }
        }
        catch (Exception ex)
        {
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            Console.WriteLine("Error: " + ex.Message);
        }
        context.Response.Close();
    }

    public void CopyFile(HttpListenerContext context, string sourceFileName, string destinationDirectory)
    {
        
       
        string sourceFilePath = Path.Combine(FILE_STORAGE_PATH, sourceFileName);
        int lastIndex = sourceFileName.LastIndexOf("/");
        if (lastIndex !=  -1)
        {
            sourceFileName = sourceFileName.Substring(lastIndex+1);
        }
        //string destinationFilePath = Path.Combine(FILE_STORAGE_PATH, destinationDirectory, sourceFileName);
        string destinationFilePath = Path.Combine(FILE_STORAGE_PATH, destinationDirectory);
        if (destinationDirectory == "...")
        { 
            destinationFilePath = Path.Combine(FILE_STORAGE_PATH_CONST, sourceFileName);
        }
        try
        {
            if (File.Exists(sourceFilePath)) // вот в этот момент фолс и 404
            {
                string destinationDirPath = Path.Combine(FILE_STORAGE_PATH, destinationDirectory);
                //убрал нахрен проверку так как выбрасывает
/*
                if (!Directory.Exists(destinationDirPath))
                {
                    context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                    context.Response.Close();
                    return;
                }
*/
                int count = 1;
                string destinationFileName = Path.GetFileName(destinationFilePath);
                while (File.Exists(destinationFilePath))
                {
                    string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(destinationFileName);
                    string fileExtension = Path.GetExtension(destinationFileName);
                    destinationFileName = $"{fileNameWithoutExtension}_{count}{fileExtension}";
                    destinationFilePath = Path.Combine(FILE_STORAGE_PATH, destinationDirectory, destinationFileName);
                    count++;
                }

                File.Copy(sourceFilePath, destinationFilePath);
                context.Response.StatusCode = (int)HttpStatusCode.Created;
            }
            else
            {
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
            }
        }
        catch (Exception ex)
        {
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            Console.WriteLine("Error: " + ex.Message);
        }
        context.Response.Close();
    }


    public void MoveFile(HttpListenerContext context, string sourceFileName, string destinationDirectory)
    {
       
        string sourceFilePath = Path.Combine(FILE_STORAGE_PATH, sourceFileName);
        int lastIndex = sourceFileName.LastIndexOf("/");
        if (lastIndex !=  -1)
        {
            sourceFileName = sourceFileName.Substring(lastIndex+1);
        }
        //string destinationFilePath = Path.Combine(FILE_STORAGE_PATH, destinationDirectory, sourceFileName); //!!!
        string destinationFilePath = Path.Combine(FILE_STORAGE_PATH, destinationDirectory);
        if (destinationDirectory == "...")
        { 
            destinationFilePath = Path.Combine(FILE_STORAGE_PATH_CONST, sourceFileName);
        }
        try
        {
            if (File.Exists(sourceFilePath))
            {
                string destinationDirPath = Path.Combine(FILE_STORAGE_PATH, destinationDirectory);
                //убрал нахрен проверку так как выбрасывает
/*
                if (!Directory.Exists(destinationDirPath))
                {
                    context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                    context.Response.Close();
                    return;
                }
*/
                int count = 1;
                string destinationFileName = Path.GetFileName(destinationFilePath);
                while (File.Exists(destinationFilePath))
                {
                    string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(destinationFileName);
                    string fileExtension = Path.GetExtension(destinationFileName);
                    destinationFileName = $"{fileNameWithoutExtension}_{count}{fileExtension}";
                    destinationFilePath = Path.Combine(FILE_STORAGE_PATH, destinationDirectory, destinationFileName);
                    count++;
                }

                File.Move(sourceFilePath, destinationFilePath);
                context.Response.StatusCode = (int)HttpStatusCode.Created;
            }
            else
            {
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
            }
        }
        catch (Exception ex)
        {
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            Console.WriteLine("Error: " + ex.Message);
        }
        context.Response.Close();
    }
    public void DeleteDirectory(HttpListenerContext context, string directoryName)
    {
        string directoryPath = Path.Combine(FILE_STORAGE_PATH, directoryName);
        try
        {
            if (Directory.Exists(directoryPath))
            {
                Directory.Delete(directoryPath, true);
                context.Response.StatusCode = (int)HttpStatusCode.OK;
            }
            else
            {
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
            }
        }
        catch (Exception ex)
        {
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            Console.WriteLine("Error: " + ex.Message);
        }
        context.Response.Close();
    }
    public void MakeDirectory(HttpListenerContext context, string directoryName)
    {
        string directoryPath = Path.Combine(FILE_STORAGE_PATH, directoryName);
        try
        {
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
                context.Response.StatusCode = (int)HttpStatusCode.Created;
            }
            else
            {
                context.Response.StatusCode = (int)HttpStatusCode.Conflict;
            }
        }
        catch (Exception ex)
        {
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            Console.WriteLine("Error: " + ex.Message);
        }
        context.Response.Close();
    }

    public void ChangeDirectory(HttpListenerContext context, string directory)
    {
        try
        {
            if (directory == "..")
            {
                string parentPath = Directory.GetParent(FILE_STORAGE_PATH)?.FullName;
                if (IsPathInsideStorage(parentPath) && parentPath.Contains(FILE_STORAGE_PATH_CONST))
                {
                    FILE_STORAGE_PATH = parentPath;
                    context.Response.StatusCode = (int)HttpStatusCode.OK;
                }
                else
                {
                    context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                    Console.WriteLine("Access to parent directory is forbidden.");
                }
            }
            else
            {
                string directoryPath = Path.Combine(FILE_STORAGE_PATH, directory);
                if (Directory.Exists(directoryPath))
                {
                    FILE_STORAGE_PATH = directoryPath;
                    context.Response.StatusCode = (int)HttpStatusCode.OK;
                }
                else
                {
                    context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                    Console.WriteLine("Directory not found.");
                }
            }
        }
        catch (Exception ex)
        {
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            Console.WriteLine("Error: " + ex.Message);
        }
        context.Response.Close();
    }
 public void ListFilesRecursive(HttpListenerContext context)
 {
     try
     {
         string response = ListDirectoryContents( FILE_STORAGE_PATH_CONST);
         byte[] buffer = Encoding.UTF8.GetBytes(response);
 
         context.Response.StatusCode = (int)HttpStatusCode.OK;
         context.Response.ContentType = "text/plain";
         context.Response.ContentLength64 = buffer.Length;
         context.Response.OutputStream.Write(buffer, 0, buffer.Length);
     }
     catch (Exception ex)
     {
         context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
         Console.WriteLine("Error: " + ex.Message);
     }
     context.Response.Close();
 }
 
 private string ListDirectoryContents(string rootPath, string indent = "")
 {
     StringBuilder sb = new StringBuilder();
     DirectoryInfo directory = new DirectoryInfo(rootPath);
 
    
     if (rootPath == FILE_STORAGE_PATH)
     {
         sb.AppendLine($"---------- you are here ({directory.Name}):");
     }
     else
     {
         directory = new DirectoryInfo(rootPath);
         sb.AppendLine($"{indent}({directory.Name}):");
     }
 
     foreach (var file in directory.GetFiles())
     {
         sb.AppendLine($"{indent}- {file.Name}");
     }
 
     foreach (var subDirectory in directory.GetDirectories())
     {
         sb.Append(ListDirectoryContents(subDirectory.FullName, indent + "  "));
     }
 
     return sb.ToString();
 }
public void RenameFile(HttpListenerContext context, string currentFileName, string newFileName)
    {
        string currentFilePath = Path.Combine(FILE_STORAGE_PATH, currentFileName);
        string newFilePath = Path.Combine(FILE_STORAGE_PATH, newFileName);
        try
        {
            if (File.Exists(currentFilePath))
            {
                if (File.Exists(newFilePath))
                {
              
                    string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(newFileName);
                    string fileExtension = Path.GetExtension(newFileName);
                    int count = 1;
                    string uniqueFileName = $"{fileNameWithoutExtension}_{count}{fileExtension}";
                    while (File.Exists(Path.Combine(FILE_STORAGE_PATH, uniqueFileName)))
                    {
                        count++;
                        uniqueFileName = $"{fileNameWithoutExtension}_{count}{fileExtension}";
                    }
                    newFileName = uniqueFileName;
                    newFilePath = Path.Combine(FILE_STORAGE_PATH, newFileName);
                }
                File.Move(currentFilePath, newFilePath);
                context.Response.StatusCode = (int)HttpStatusCode.OK;
            }
            else
            {
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
            }
        }
        catch (Exception ex)
        {
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            Console.WriteLine("Error: " + ex.Message);
        }
        context.Response.Close();
    }

    public void RenameDirectory(HttpListenerContext context, string currentDirectoryName, string newDirectoryName)
    {
        string currentDirectoryPath = Path.Combine(FILE_STORAGE_PATH, currentDirectoryName);
        string newDirectoryPath = Path.Combine(FILE_STORAGE_PATH, newDirectoryName);
        try
        {
            if (Directory.Exists(currentDirectoryPath))
            {
                if (Directory.Exists(newDirectoryPath))
                {
                    int count = 1;
                    string uniqueDirectoryName = $"{newDirectoryName}_{count}";
                    while (Directory.Exists(Path.Combine(FILE_STORAGE_PATH, uniqueDirectoryName)))
                    {
                        count++;
                        uniqueDirectoryName = $"{newDirectoryName}_{count}";
                    }
                    newDirectoryName = uniqueDirectoryName;
                    newDirectoryPath = Path.Combine(FILE_STORAGE_PATH, newDirectoryName);
                }
                Directory.Move(currentDirectoryPath, newDirectoryPath);
                context.Response.StatusCode = (int)HttpStatusCode.OK;
            }
            else
            {
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
            }
        }
        catch (Exception ex)
        {
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            Console.WriteLine("Error: " + ex.Message);
        }
        context.Response.Close();
    }
    public void Stop()
    {
        listener.Stop();
    }
}

class Program
{
    static void Main(string[] args)
    {
        const string SERVER_URL = "http://localhost:8080/";
        //const string SERVER_URL = "http://192.168.1.54:8080/";
        //192.168.1.54

        TextFileServer server = new TextFileServer(SERVER_URL);
        server.Start();

        Console.WriteLine("Press any key to stop the server...");
        Console.ReadKey();

        server.Stop();
    }
}
