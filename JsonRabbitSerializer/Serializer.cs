using System;
using System.Xml;
using System.Xml.XPath;
using Microsoft.AspNetCore.Http;
using Microsoft.Net;
using Microsoft.Net.Http;
using System.IO;
using Newtonsoft.Json.Linq;
using System.Linq;
namespace JsonRabbitSerializer
{
    public class Serializer
    {
        public static XmlDocument CreateRabbitMessage(HttpContext context)
        {
            XmlDocument xDoc = new XmlDocument();
            XmlElement topNode = xDoc.CreateElement("HttpWrapper");
            xDoc.AppendChild(topNode);
            XmlElement headers = xDoc.CreateElement("headers");
            topNode.AppendChild(headers);
            foreach (string h in context.Request.Headers.Keys)
            {
                XmlElement header = xDoc.CreateElement("header");
                XmlElement key = xDoc.CreateElement("key");
                key.InnerText = h;
                XmlElement val = xDoc.CreateElement("value");
                val.InnerText = context.Request.Headers[h];
                header.AppendChild(key);
                header.AppendChild(val);
                headers.AppendChild(header);
            }

            XmlElement cookies = xDoc.CreateElement("cookies");
            topNode.AppendChild(cookies);
            foreach (var c in context.Request.Cookies)
            {
                XmlElement cookie = xDoc.CreateElement("cookie");
                XmlElement key = xDoc.CreateElement("key");
                key.InnerText = c.Key;
                XmlElement val = xDoc.CreateElement("value");
                val.InnerText = c.Value;
                cookie.AppendChild(key);
                cookie.AppendChild(val);
                cookies.AppendChild(cookie);
            }
            XmlElement method = xDoc.CreateElement("HttpMethod");
            method.InnerText = context.Request.Method;
            topNode.AppendChild(method);

            XmlElement Scheme = xDoc.CreateElement("HttpScheme");
            Scheme.InnerText = context.Request.Scheme;
            topNode.AppendChild(Scheme);

            XmlElement QueryString = xDoc.CreateElement("QueryString");
            QueryString.InnerText = context.Request.QueryString.Value;
            topNode.AppendChild(QueryString);

            XmlElement Path = xDoc.CreateElement("Path");
            Path.InnerText = context.Request.Path;
            topNode.AppendChild(Path);

            XmlElement ContentType = xDoc.CreateElement("ContentType");
            ContentType.InnerText = context.Request.ContentType;
            topNode.AppendChild(ContentType);

            XmlElement ContentLength = xDoc.CreateElement("ContentLength");
            ContentLength.InnerText = context.Request.ContentLength.ToString();
            topNode.AppendChild(ContentLength);

            XmlElement Protocol = xDoc.CreateElement("Protocol");
            Protocol.InnerText = context.Request.Protocol;
            topNode.AppendChild(Protocol);

            XmlElement Body = xDoc.CreateElement("Body");
            if (context.Request.Body != null)
            {
                StreamReader reader = new StreamReader(context.Request.Body, System.Text.Encoding.UTF8);
                Body.InnerText = reader.ReadToEnd();
                reader.Close();
            }
            else
                Body.InnerText = string.Empty;
            topNode.AppendChild(Body);
            return xDoc;
        }

        public static XmlDocument CreateRabbitResult(System.Net.Http.HttpResponseMessage  context)
        {
            XmlDocument xDoc = new XmlDocument();
            XmlElement topNode = xDoc.CreateElement("HttpWrapper");
            xDoc.AppendChild(topNode);
            XmlElement headers = xDoc.CreateElement("headers");
            topNode.AppendChild(headers);
            foreach (var h in context.Headers)
            {
                XmlElement header = xDoc.CreateElement("header");
                XmlElement key = xDoc.CreateElement("key");
                key.InnerText = h.Key;
                XmlElement values = xDoc.CreateElement("values");
                foreach(string strVal in h.Value)
                {
                    XmlElement value = xDoc.CreateElement("value");
                    value.InnerText = strVal;
                    values.AppendChild(value);
                }
                
                header.AppendChild(key);
                header.AppendChild(values);
                headers.AppendChild(header);
            }

            //XmlElement cookies = xDoc.CreateElement("cookies");
            //topNode.AppendChild(cookies);
            //foreach (var c in context.Cookies)
            //{
            //    XmlElement cookie = xDoc.CreateElement("cookie");
            //    XmlElement key = xDoc.CreateElement("key");
            //    key.InnerText = c.Key;
            //    XmlElement val = xDoc.CreateElement("value");
            //    val.InnerText = c.Value;
            //    cookie.AppendChild(key);
            //    cookie.AppendChild(val);
            //    cookies.AppendChild(cookie);
            //}
            XmlElement statusCode = xDoc.CreateElement("StatusCode");
            statusCode.InnerText = ((int) context.StatusCode).ToString();
            topNode.AppendChild(statusCode);

            
            XmlElement ContentType = xDoc.CreateElement("ContentType");
            if (context.Content.Headers.ContentType != null)
            {
                ContentType.InnerText = context.Content.Headers.ContentType.ToString();
                topNode.AppendChild(ContentType);
            }

            XmlElement ContentLength = xDoc.CreateElement("ContentLength");
            if (context.Content.Headers.ContentLength != null)
            {
                ContentLength.InnerText = context.Content.Headers.ContentLength.ToString();
                topNode.AppendChild(ContentLength);
            }

            XmlElement Body = xDoc.CreateElement("Body");
            try
            {
                StreamReader reader = new StreamReader(context.Content.ReadAsStreamAsync().Result, System.Text.Encoding.UTF8);
                var encoding = reader.CurrentEncoding;
                Body.InnerText = reader.ReadToEnd();
                reader.Close();
            }
            catch(Exception e)
            {
                Console.WriteLine("Serializer | CreateRabbitResult | Exception: " + e.Message);
                Body.InnerText = string.Empty;
            }

            topNode.AppendChild(Body);
            return xDoc;
        }

        public static XmlDocument CreateRabbitError(int theStatusCode, string content)
        {
            XmlDocument xDoc = new XmlDocument();
            XmlElement topNode = xDoc.CreateElement("HttpWrapper");
            xDoc.AppendChild(topNode);

            XmlElement headers = xDoc.CreateElement("headers");
            topNode.AppendChild(headers);
            XmlElement statusCode = xDoc.CreateElement("StatusCode");
            statusCode.InnerText = theStatusCode.ToString();
            topNode.AppendChild(statusCode);

            XmlElement ContentType = xDoc.CreateElement("ContentType");
            ContentType.InnerText = "application/json";
            topNode.AppendChild(ContentType);
            XmlElement ContentLength = xDoc.CreateElement("ContentLength");

         

            XmlElement Body = xDoc.CreateElement("Body");
            try
            {
                Body.InnerText = content;
            }
            catch (Exception e)
            {
                Console.WriteLine("Serializer | CreateRabbitError | Exception: " + e.Message);
                Body.InnerText = string.Empty;
            }

            topNode.AppendChild(Body);
            return xDoc;
        }

        public static XmlDocument CreateRabbitErrorFromException(int theStatusCode, Exception ex)
        {
            string content = string.Empty;
            try
            {
                string errorMessage = ex.Message;
                string source = ex.Source;
                string stacktrace = ex.StackTrace;
                string innerexception = "";
                if (ex.InnerException != null)
                    innerexception = ex.InnerException.Message;
                JObject errJson = new JObject(
                    new JProperty("type", "RabbitRPCServerException"),
                    new JProperty("errormessage", errorMessage),
                    new JProperty("source", source),
                    new JProperty("stacktrace", stacktrace),
                    new JProperty("innerexception", innerexception));
                content = errJson.ToString();

            }
            catch (Exception e)
            {
                string errorMessage = e.Message;
                string source = e.Source;

                JObject errJson = new JObject(
                    new JProperty("type", "RabbitRPCServerInternalError"),
                    new JProperty("errormessage", errorMessage),
                    new JProperty("source", source));
                content = errJson.ToString();

            }

            return CreateRabbitError(theStatusCode, content);
        }
    }
}
