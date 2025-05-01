using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;


namespace ExpertMed.Services
{
    public class ChatGPTService
    {
        private readonly string apiKey = "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"; // Reemplaza con tu API Key
        private readonly string endpoint = "https://api.openai.com/v1/chat/completions";

        public async Task<string> GetChatGPTResponse(string userInput)
        {
            using (var client = new HttpClient())
            {
                // Configurar autorización
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

                // Construir el cuerpo de la solicitud
                var requestBody = new
                {
                    model = "gpt-3.5", // O "gpt-4" si lo prefieres y está habilitado para ti
                    messages = new object[]
                    {
                    // Mensaje de sistema para establecer el comportamiento del bot
                    new { role = "system", content = "Eres un asistente médico. Recuerda incluir siempre el siguiente disclaimer en tus respuestas: 'No soy un médico, esta información es solo para fines informativos.'" },
                    // Mensaje del usuario con la consulta médica
                    new { role = "user", content = userInput }
                    }
                };

                var jsonRequestBody = JsonConvert.SerializeObject(requestBody);
                var contentData = new StringContent(jsonRequestBody, Encoding.UTF8, "application/json");

                // Enviar la solicitud
                var response = await client.PostAsync(endpoint, contentData);
                response.EnsureSuccessStatusCode(); // Lanza excepción si hay error

                var responseString = await response.Content.ReadAsStringAsync();

                // Deserializar la respuesta
                var chatResponse = JsonConvert.DeserializeObject<ChatGPTResponse>(responseString);

                // Retornar la respuesta del modelo
                return chatResponse?.choices?[0]?.message?.content ?? "No se obtuvo respuesta.";
            }
        }
        // Clases para parsear la respuesta de OpenAI
        public class ChatGPTResponse
        {
            public Choice[] choices { get; set; }
        }

        public class Choice
        {
            public Message message { get; set; }
        }
        public class AskRequest
        {
            public string Pregunta { get; set; }
        }

        public class Message
        {
            public string role { get; set; }
            public string content { get; set; }
        }


    }
}