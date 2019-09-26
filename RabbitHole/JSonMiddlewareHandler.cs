using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using System.Xml;
using System.IO;
using System.Linq;
using System.Diagnostics;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace RabbitHole
{
    class JSonMiddlewareHandler
    {
        private readonly string hostname = Environment.GetEnvironmentVariable("CONNECTIONSTRING_ACTIVITYLOG");

        // Must have constructor with this signature, otherwise exception at run time
        public JSonMiddlewareHandler(RequestDelegate next)
        {
            // This is an HTTP Handler, so no need to store next
        }

        public async Task Invoke(HttpContext context)
        {
            if (context.Request.Method == "OPTIONS")
            {
                context.Response.StatusCode = 200;
                string acceptedHeaders = "accept,content-type,Token";
                if (context.Request.Headers.ContainsKey("Access-Control-Request-Headers"))
                    acceptedHeaders = context.Request.Headers["Access-Control-Request-Headers"];
                context.Response.Headers.Add("Access-Control-Allow-Headers",acceptedHeaders);
                context.Response.Headers.Add("Access-Control-Allow-Methods", "POST,PUT,GET,DELETE,OPTIONS");
                context.Response.Headers.Add("Access-Control-Allow-Origin", "*");
                context.Response.Headers.Add("Access-Control-Max-Age", "60");
            }
            else
            {
                XmlDocument xDoc = JsonRabbitSerializer.Serializer.CreateRabbitMessage(context);

                string response = GenerateResponse(ref context, xDoc);

                await context.Response.WriteAsync(response, Encoding.UTF8);
            }
        }

        // ...

        private string GenerateResponse(ref HttpContext  context, XmlDocument xDoc)
        {
            // Initial log.
            string requestUri = context.Request.Path.ToString() + "/" + context.Request.QueryString.ToString();
            var odsHeader = context.Request.Headers.FirstOrDefault(item => item.Key == "X-PICASO-ODS");
            int logId = InitialLog(context.Request.Method, requestUri, context.Request.ContentType, context.Request.Headers, odsHeader.Value);
            //var stopWatch = new Stopwatch();
            //stopWatch.Start();

            var rpcClient = new RpcClient();
            //string rpcQueue = GetODSQueueName(context.Request);
            //if (string.IsNullOrEmpty(rpcQueue))
            //    return null;
            string rpcQueue = "rpc_queue";
            var response = rpcClient.Call(rpcQueue, xDoc.OuterXml);
            rpcClient.Close();
            int statusCode = -1;
            string contentType = string.Empty;
            string message = string.Empty;

            XmlDocument incomingDoc = new XmlDocument();
            try
            {
                incomingDoc.LoadXml(response);

                // Update log
                //stopWatch.Stop();
                context.Response.Headers.Clear();
                if (incomingDoc.SelectSingleNode("//ContentType") != null)
                    context.Response.ContentType = contentType = incomingDoc.SelectSingleNode("//ContentType").InnerText;
                XmlNode statusCodeNode = incomingDoc.SelectSingleNode("//StatusCode");
                if (statusCodeNode != null)
                    int.TryParse(statusCodeNode.InnerText, out statusCode);
                XmlNode bodyNode = incomingDoc.SelectSingleNode("//Body");
                //if(bodyNode != null)
                //    UpdateLog(logId, contentType, statusCodeNode, bodyNode, null);
                //else
                //    UpdateLog(logId, contentType, statusCodeNode, null, null);


                //Set up Cookies

                //XmlNodeList xCookies = incomingDoc.SelectNodes("//cookies/cookie");
                //foreach (XmlNode xCookie in xCookies)
                //{
                //    context.Response.Cookies.Append(xCookie.SelectSingleNode(".//key").InnerText, xCookie.SelectSingleNode(".//value").InnerText);
                //}

                XmlNodeList xHeaders = incomingDoc.SelectNodes("//headers/header");
                
                foreach (XmlNode xHeader in xHeaders)
                {
                    string key = xHeader.SelectSingleNode(".//key").InnerText; 
                    string value = xHeader.SelectSingleNode(".//value").InnerText;
                    if (key == "Cookie" || key == "Content-Type" || key == "Content-Length" || key == "Accept-Encoding") ;
                    // Picaso based custom headers
                    else if (key == "X-PICASO-RequesterUPID" || key == "X-PICASO-RequesteeUPID" || key == "X-PICASO-ODS")
                        context.Response.Headers.Add(key, value);
                    //else
                    //{
                    //    //if (key == "Host")
                    //    //  value = domain;
                    //    context.Response.Headers.Add(key, value);
                    //}
                }

                string body = string.Empty;
                if (incomingDoc.SelectSingleNode("//Body") != null)
                {
                    if (contentType != "text/html")
                        body = incomingDoc.SelectSingleNode("//Body").InnerText;
                    else if (incomingDoc.SelectSingleNode("//Body").InnerText.Contains("blocked for security reasons"))
                    {
                        message = "Blocked for security reasons";
                        context.Response.Headers.Add("Content-Type", "application/json");
                        dynamic errorBody = new JObject();
                        errorBody.Result = "AR";
                        errorBody.Message = message;
                        body = JsonConvert.SerializeObject(errorBody);
                    }
                }

                if (statusCode == 500)
                {
                    dynamic bodyObj = JsonConvert.DeserializeObject(body);
                    UpdateLog(logId, statusCode, bodyObj.errorMessage, bodyObj.innerexception);
                }
                else if (statusCode == 201 || statusCode == 400 || statusCode == 401 || statusCode == 501 || statusCode == 502)
                    UpdateLog(logId, statusCode, body, null);
                else
                    UpdateLog(logId, statusCode, message, null);

                context.Response.StatusCode = statusCode;
                return body;
            }
            catch (Exception e)
            {
                Console.WriteLine("JsonMiddlewareHandler | GenerateResponse | Exception: " + e.Message);
                UpdateLog(logId, statusCode, e.Message, e.InnerException.ToString());
                return e.Message;
            }
        }

        private string GetContentType()
        {
            return "application/xml";
        }

        /// <summary>
        /// Get different queue name depending on X-PICASO-ODS header. Return empty if no match.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns></returns>
        private string GetODSQueueName(HttpRequest request)
        {
            var header = request.Headers.FirstOrDefault(item => item.Key.Equals("X-PICASO-ODS", StringComparison.OrdinalIgnoreCase));
            if (header.Key != null && header.Value != string.Empty)
            {
                string headerStr = header.Value;
                if (headerStr.ToLower() == "cnet")
                    return "rpc_" + headerStr.ToLower();
                else if (headerStr.ToLower() == "udus")
                    return "rpc_" + headerStr.ToLower();
                else if (headerStr.ToLower() == "utv")
                    return "rpc_" + headerStr.ToLower();
                else
                    return string.Empty;
            }
            else
                return null;
        }

        private int InitialLog(string method, string request, string contentType, IHeaderDictionary header, string ods)
        {
            int id = -1;

            string headers = GetHeaders(header);

            using (Repository.PicasoActivityLogContext context = new Repository.PicasoActivityLogContext())
            {
                var sw = new Stopwatch();
                sw.Start();
                var log = new Repository.RabbitHoleLog();
                // Request
                log.Method = method;
                log.Request = request;
                log.ContentType = contentType;
                log.PicasoHeader = headers;
                log.ODS = ods;
                log.Timestamp = DateTime.UtcNow;
                log.Duration = -1;
                log.StatusCode = -1;
                context.RabbitHoleLog.Add(log);
                context.SaveChanges();
                id = log.id;
            }

            return id;
        }

        private string GetHeaders(IHeaderDictionary headers)
        {
            StringBuilder sb = new StringBuilder();

            if(headers != null && headers.Count > 0)
            {
                var theHeaders = headers.Where(item => item.Key.Contains("PICASO")).ToList();
                for (int i = 0; i < theHeaders.Count; i++)
                {
                    sb.Append(theHeaders[i].Key + ":" + theHeaders[i].Value);
                    if (i != theHeaders.Count - 1)
                        sb.Append(",");
                }
            }

            return sb.ToString();
        }

        private bool UpdateLog(int id, string contentType, XmlNode statusCodeNode, XmlNode bodyNode, string exception)
        {
            bool result = false;

            int statusCode = -1;
            if(statusCodeNode != null)
                int.TryParse(statusCodeNode.InnerText, out statusCode);

            string message = null;
            if (contentType == "application/json" || contentType == "text/plain")
            {
                if (bodyNode != null && (statusCode == 400 || statusCode == 401 || statusCode == 500))
                {
                    message = bodyNode.InnerText;
                }
            }

            using (Repository.PicasoActivityLogContext context = new Repository.PicasoActivityLogContext())
            {
                // Response
                var theLog = context.RabbitHoleLog.FirstOrDefault(item => item.id == id);
                if (theLog != null)
                {
                    TimeSpan ts = DateTime.UtcNow - theLog.Timestamp;
                    theLog.StatusCode = statusCode;
                    theLog.Message = message;
                    theLog.Duration = Math.Round((ts.TotalMilliseconds / 1000), 4);
                    context.SaveChanges();
                }
            }

            return result;
        }

        private bool UpdateLog(int id, int statusCode, string message, string exception)
        {
            bool result = false;

            using (Repository.PicasoActivityLogContext context = new Repository.PicasoActivityLogContext())
            {
                // Response
                var theLog = context.RabbitHoleLog.FirstOrDefault(item => item.id == id);
                if (theLog != null)
                {
                    TimeSpan ts = DateTime.UtcNow - theLog.Timestamp;
                    theLog.StatusCode = statusCode;
                    theLog.Message = message;
                    theLog.Duration = Math.Round((ts.TotalMilliseconds / 1000), 4);
                    context.SaveChanges();
                }
            }

            return result;
        }
    }

    public static class JSonMiddlewareHandlerExtensions
    {
        public static IApplicationBuilder UseMyHandler(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<JSonMiddlewareHandler>();
        }
    }


}
