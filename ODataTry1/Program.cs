using Microsoft.Data.OData;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ODataTry1
{
    class Program
    {
        static void Main(string[] args)
        {
            var test = new ODataTest();
            string encodedEntity = test.Encode();
            Console.WriteLine("Encoded Entity: {0}", encodedEntity);
            test.Decode(encodedEntity);
            Console.ReadKey();
        }
    }

    class ODataRequest : IODataRequestMessage
    {
        private Stream stream = new MemoryStream();
        private IDictionary<string, string> headers = new Dictionary<string, string>();
        private string method = "POST";
        private Uri url = new Uri("http://odata.netflix.com/v2/Catalog/Genres");

        public ODataRequest()
        {
            headers["DataServiceVersion"] = "3.0";
            headers["MaxDataServiceVersion"] = "3.0";
        }

        public ODataRequest(Stream stream) : this()
        {
            this.stream = stream;
        }

        string IODataRequestMessage.GetHeader(string headerName)
        {
            if (headers.ContainsKey(headerName))
            {
                return headers[headerName];
            }
            else
            {
                return null;
            }
        }

        Stream IODataRequestMessage.GetStream()
        {
            return stream;
        }

        IEnumerable<KeyValuePair<string, string>> IODataRequestMessage.Headers
        {
            get { return headers; }
        }

        string IODataRequestMessage.Method
        {
            get
            {
                return method;
            }
            set
            {
                method = value;
            }
        }

        void IODataRequestMessage.SetHeader(string headerName, string headerValue)
        {
            headers[headerName] = headerValue;
        }

        Uri IODataRequestMessage.Url
        {
            get
            {
                return url;
            }
            set
            {
                url = value;
            }
        }
    }

    class ODataTest
    {
        public void Decode(string data)
        {
            MemoryStream ms = new MemoryStream(System.Text.Encoding.ASCII.GetBytes(data));
            IODataRequestMessage request = new ODataRequest(ms);
            request.SetHeader("Content-Type", "application/atom+xml");


            ODataMessageReader omr = new ODataMessageReader(request);
            ODataReader odr = omr.CreateODataEntryReader();
            while (odr.Read())
            {
                switch (odr.State)
                {
                    case ODataReaderState.EntryEnd:
                        ODataEntry entry = (ODataEntry)odr.Item;

                        // do whatever with the entry
                        PrintEntry(entry);

                        break;
                }
            }
        }

        private static void PrintEntry(ODataEntry entry)
        {
            Console.WriteLine("TypeName: "
                + (entry.TypeName ?? "<null>"));
            Console.WriteLine("Id: "
                + (entry.Id ?? "<null>"));
            if (entry.ReadLink != null)
            {
                Console.WriteLine("ReadLink: "
                    + entry.ReadLink.AbsoluteUri);
            }

            if (entry.EditLink != null)
            {
                Console.WriteLine("EditLink: "
                    + entry.EditLink.AbsoluteUri);
            }

            Console.WriteLine("Properties:");
            foreach (ODataProperty prop in entry.Properties)
            {
                Console.WriteLine("Name:{0} Value:{1}", prop.Name, prop.Value);
            }
        }

        public string Encode()
        {
            IODataRequestMessage request = new ODataRequest();
            request.SetHeader("Accept", "application/atom+xml");

            ODataMessageWriterSettings writerSettings =
                new ODataMessageWriterSettings()
                {
                    Indent = true,                          //pretty printing
                    CheckCharacters = false,                //sets this flag on the XmlWriter for ATOM
                    BaseUri = new Uri("http://dima.com/"),  //set the base uri to use in relative links
                    Version = ODataVersion.V3               //set the Odata version to use when writing the entry
                };
            writerSettings.SetContentType(ODataFormat.Atom);


            //create message writing for the message
            ODataMessageWriter messageWriter = new ODataMessageWriter(request, writerSettings);
            //creates a streaming writer for a single entity
            ODataWriter writer = messageWriter.CreateODataEntryWriter();

            // start the entry
            writer.WriteStart(new ODataEntry()
            {
                // the edit link is relative to the 
                //baseUri set on the writer in the case
                EditLink = new Uri("/Customers('" + "DimaCustomerId" +"')", UriKind.Relative),
                Id = "Customers('" + "DimaCustomerId" + "')",
                TypeName = "NORTHWNDModel.Customer",
                Properties = new List<ODataProperty>(){
                        new ODataProperty(){ Name = "CustomerID", Value = "DimaCustomerId"},
                        new ODataProperty(){ Name = "CompanyName", Value = "DimaCustomerName"},
                        new ODataProperty(){ Name = "ContactName", Value = "DimaContactName"},
                        new ODataProperty(){ Name = "ContactTitle", Value = "DimaContactTitle"}
                    }
            });

            writer.WriteEnd(); //tells the writer we are done writing the entity
            writer.Flush();    //always flush at the end

            // my dirty part, convert stream to string
            request.GetStream().Position = 0;
            var sr = new StreamReader(request.GetStream());
            var myStr = sr.ReadToEnd();
            return myStr;
        }
    }
}