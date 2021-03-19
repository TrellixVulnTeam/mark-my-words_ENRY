﻿using System;

using System.Collections.Generic;

using System.Text;
using System.Text.Json;

using System.Threading.Tasks;
using System.IO;

using System.Net.Http;
using System.Net.Http.Json;
using System.Net.Http.Headers;

using MarkMyWords.Shared;

using Microsoft.AspNetCore.Components;

namespace MarkMyWords.Client.Shared
{
    public static class ServerCommunicator
    {
        private const string MediaType = "application/json";

        private enum HttpMethodEnum
        {
            Post,
            Put,
            Patch
        }

        /// <summary>
        /// Creates a new assignment via HTTP POST to api/assignment.
        /// </summary>
        /// <param name="navMan"></param>
        /// <param name="assignment"></param>
        /// <returns></returns>
        public static async Task<bool> NewAssignment(NavigationManager navMan, AssignmentModel assignment, string password = null)
        {
            string destinationUri = $"{navMan.BaseUri}api/assignment";

            HttpClient client = new HttpClient();
            if (password != null)
            {
                client.DefaultRequestHeaders.Add("password", password);
            }

            HttpResponseMessage response = await client.PostAsJsonAsync(destinationUri, assignment, Utils.DefaultOptions());

            return response.IsSuccessStatusCode;
        }

        /// <summary>
        /// Updates a single assignment via HTTP PUT to api/assignment.
        /// </summary>
        /// <param name="navMan"></param>
        /// <param name="assignment"></param>
        /// <returns></returns>
        public static async Task<bool> UpdateAssignment(NavigationManager navMan, AssignmentModel assignment, AttemptModel attempt, string password = null)
        {
            string destinationUri = $"{navMan.BaseUri}api/assignment/{attempt.AttemptId}";

            JsonSerializerOptions options = Utils.DefaultOptions();

            HttpClient client = new HttpClient();
            if (password != null) 
            {
                client.DefaultRequestHeaders.Add("password", password);
            }

            HttpResponseMessage response = await client.PutAsJsonAsync(destinationUri, assignment, options);

            return response.IsSuccessStatusCode;
        }

        public static async Task<bool> UpdateAssignmentProperties(NavigationManager navMan, AssignmentModel assignment)
        {
            string destinationUri = $"{navMan.BaseUri}api/assignment";

            HttpResponseMessage response = await Upload(assignment, destinationUri, Utils.DefaultOptions(), method: HttpMethodEnum.Patch);

            return response.IsSuccessStatusCode;
        }

        /// <summary>
        /// Makes a request to api/assignment for a list of all assignments in storage.
        /// </summary>
        /// <param name="navMan"></param>
        /// <returns></returns>
        public static async Task<List<Dictionary<string, string>>> ListAssignments(NavigationManager navMan)
        {
            string destinationUri = $"{navMan.BaseUri}api/assignment";

            HttpClient client = new HttpClient();

            List<Dictionary<string, string>> assignments = await client.GetFromJsonAsync<List<Dictionary<string, string>>>(destinationUri, Utils.DefaultOptions());

            return assignments;
        }

        /// <summary>
        /// Fetches a single assignment via HTTP GET from api/assignment/{assignmentId}.
        /// </summary>
        /// <param name="navMan"></param>
        /// <param name="assignmentId"></param>
        /// <returns></returns>
        public static async Task<AssignmentModel> FetchAssignment(NavigationManager navMan, string assignmentId, string password = null)
        {
            string destinationUri = $"{navMan.BaseUri}api/assignment/{assignmentId}";

            HttpClient client = new HttpClient();
            if(password != null)
            {
                client.DefaultRequestHeaders.Add("password", password);
            }

            try
            {
                AssignmentModel assignmentModel = await client.GetFromJsonAsync<AssignmentModel>(destinationUri, Utils.DefaultOptions());
                return assignmentModel;
            }
            catch
            {
                return null;
            }
        }

        public static async Task<bool> UpdateLock(NavigationManager navMan, string assignmentId, string attemptId, bool locked)
        {
            string destinationUri = $"{navMan.BaseUri}api/lock";

            JsonSerializerOptions options = Utils.DefaultOptions();

            HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Add("assignmentId", assignmentId);
            client.DefaultRequestHeaders.Add("attemptId", attemptId);
            client.DefaultRequestHeaders.Add("locked", locked.ToString());

            HttpResponseMessage response = await client.PostAsJsonAsync(destinationUri, "", options);

            return response.IsSuccessStatusCode;
        }

        /// <summary>
        /// Fetches the range of suggested marks via HTTP POST from api/mark.
        /// </summary>
        /// <param name="navMan"></param>
        /// <param name="assignmentId"></param>
        /// <param name="attemptId"></param>
        /// <param name="sectionId"></param>
        /// <returns></returns>
        public static async Task<Dictionary<string, float>> FetchMarkRange(NavigationManager navMan, AssignmentModel assignment, string attemptId, string sectionId)
        {
            string destinationUri = $"{navMan.BaseUri}api/mark";

            HttpClient client = new HttpClient();

            client.DefaultRequestHeaders.Add("AssignmentId", assignment.AssignmentId);
            client.DefaultRequestHeaders.Add("AttemptId", attemptId);
            client.DefaultRequestHeaders.Add("SectionId", sectionId);

            HttpResponseMessage response  = await client.PostAsJsonAsync(destinationUri, assignment, Utils.DefaultOptions());
            Dictionary<string, float> markRange = await JsonSerializer.DeserializeAsync<Dictionary<string, float>>(await response.Content.ReadAsStreamAsync(), Utils.DefaultOptions());

            return markRange;
        }

        private static async Task<Stream> Download(string uri, Dictionary<string, string> headers = default)
        {
            HttpClient client = new HttpClient();
            HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Get, uri);

            requestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(MediaType));
            if (headers != null)
            {
                foreach (KeyValuePair<string, string> header in headers)
                {
                    requestMessage.Headers.Add(header.Key, header.Value);
                }
            }

            HttpResponseMessage responseMessage = await client.SendAsync(requestMessage);

            return await responseMessage.Content.ReadAsStreamAsync();
        }

        private static async Task<HttpResponseMessage> Upload(
            object payload, 
            string uri, 
            JsonSerializerOptions serializerOptions = default, 
            HttpMethodEnum method = HttpMethodEnum.Post)
        {
            HttpClient client = new HttpClient();
            HttpMethod httpMethod = method switch
            {
                HttpMethodEnum.Put => HttpMethod.Put,
                HttpMethodEnum.Patch => HttpMethod.Patch,
                _ => HttpMethod.Post,
            };

            HttpRequestMessage requestMessage = new HttpRequestMessage(httpMethod, uri)
            {
                Content = new StringContent(JsonSerializer.Serialize(payload, serializerOptions), Encoding.UTF8, MediaType)
            };

            HttpResponseMessage responseMessage = await client.SendAsync(requestMessage);
            return responseMessage;
        }
    }
}
