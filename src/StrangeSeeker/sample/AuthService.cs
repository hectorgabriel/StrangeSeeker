namespace Sample;

public class AuthService
{
    public void LogIn()
    {
        Console.WriteLine("Logging in...");

        var httpClient = new HttpClient
        {
            BaseAddress = new Uri("https://identity.example.com")
        };
        httpClient.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

        var formData = new MultipartFormDataContent
        {
            { new StringContent("username"), "username" },
            { new StringContent("password"), "password" }
        };

        var response = httpClient.PostAsync("/login", formData);
        var result = response.Result.Content.ReadAsStringAsync().Result;
        response.EnsureSuccessStatusCode(); 

        if (response.Result.IsSuccessStatusCode)
        {
            Console.WriteLine("Login successful.");
        }
        else
        {
            Console.WriteLine("Login failed.");
        }

        Console.WriteLine($"Login response: {result}");
    }
}