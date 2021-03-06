#region License

/* Authors:
 *      Sebastien Lambla (seb@serialseb.com)
 * Copyright:
 *      (C) 2007-2009 Caffeine IT & naughtyProd Ltd (http://www.caffeine-it.com)
 * License:
 *      This file is distributed under the terms of the MIT License found at the end of this file.
 */

#endregion

using System.Text;
using OpenRasta.Codecs;
using OpenRasta.Web;
using NUnit.Framework;
using OpenRasta.IO;
using System.IO;
using System.Threading.Tasks;
using OpenRasta.Tests.Unit.Infrastructure;
using Shouldly;

namespace ApplicationOctetStreamCodec_Specification
{
    public class when_converting_a_byte_stream_to_an_ifile : applicationoctetstream_context
    {
        [Test]
        public async Task an_ifile_object_is_generated()
        {
            given_context();
            given_request_entity_stream();

            await when_decoding();

            ThenTheResult
                .ShouldNotBeNull();
        }

        [Test]
        public async Task the_length_is_set_to_the_proper_value()
        {
            given_context();
            given_request_entity_stream(1000);

            await when_decoding();

            ThenTheResult.Length.ShouldBe(1000);
        }

        [Test]
        public async Task an_ireceivedfile_object_is_generated()
        {
            given_context();
            given_request_entity_stream();

            await when_decoding();

            ThenTheResult
                .ShouldNotBeNull();
        }

        [Test]
        public async Task the_file_name_is_null_if_no_content_disposition_header_is_present()
        {
            given_context();
            given_request_entity_stream();
            given_content_disposition_header("attachment");

            await when_decoding();

            ThenTheResult.FileName.ShouldBeNull();
        }

        [Test]
        public async Task the_original_name_is_set_when_present_in_the_content_disposition_header()
        {
            given_context();
            given_request_entity_stream();
            given_content_disposition_header("attachment;filename=\"test.txt\"");

            await when_decoding();

            ThenTheResult.FileName.ShouldBe("test.txt");
        }

        public async Task when_decoding()
        {
            await when_decoding<IFile>();
        }

        public IFile ThenTheResult => then_decoding_result<IFile>();
    }

    [TestFixture]
    public class when_converting_an_ifile_to_a_byte_stream : media_type_writer_context<ApplicationOctetStreamCodec>
    {
        object _entity;

        [Test]
        public async Task a_file_without_name_doesnt_generate_a_header()
        {
            given_context();
            given_entity(new InMemoryFile());

            await when_coding();

            Response.Headers.ContentDisposition.ShouldBeNull();
        }

        [Test]
        public async Task a_file_with_a_name_generates_an_inline_content_disposition()
        {
            given_context();
            given_entity(new InMemoryFile() {FileName = "test.txt"});

            await when_coding();
            Response.Headers.ContentDisposition.ShouldNotBeNull();

            Response.Headers.ContentDisposition.Disposition.ShouldBe("inline");
            Response.Headers.ContentDisposition.FileName.ShouldBe("test.txt");
        }

        [Test]
        public async Task a_file_without_a_content_type_generates_an_app_octet_stream_content_type()
        {
            given_context();
            given_entity(new InMemoryFile());

            await when_coding();
            Response.Headers.ContentType.ShouldBe(MediaType.ApplicationOctetStream);
        }

        [Test]
        public async Task a_file_with_a_content_type_generates_the_correct_content_type_header()
        {
            given_context();
            given_entity(new InMemoryFile {ContentType = MediaType.TextPlain});

            await when_coding();
            Response.Headers.ContentType.ShouldBe(MediaType.TextPlain);
        }

        [Test]
        public async Task a_file_with_a_content_type_of_app_octet_stream_doesnt_override_response_content_type()
        {
            given_context();
            given_entity(new InMemoryFile {ContentType = MediaType.ApplicationOctetStream});
            Response.Headers.ContentType = MediaType.Xml;

            await when_coding();
            Response.Headers.ContentType.ShouldBe(MediaType.Xml);
        }

        [Test]
        public async Task a_file_with_a_more_specific_content_type_overrides_the_response_content_type()
        {
            given_context();
            given_entity(new InMemoryFile {ContentType = MediaType.Xml});
            Response.Headers.ContentType = MediaType.ApplicationOctetStream;

            await when_coding();
            Response.Headers.ContentType.ShouldBe(MediaType.Xml);
        }

        [Test]
        public async Task a_file_with_a_length_sets_the_response_length()
        {
            given_context();
            given_entity(new InMemoryFile { Length = 1029});

            await when_coding();
            Response.Headers.ContentLength.ShouldBe(1029);
        }

        [Test]
        public async Task a_downloadable_file_with_name_generates_a_content_disposition()
        {
            given_context();
            given_entity(new InMemoryDownloadableFile() {FileName = "test.txt"});

            await when_coding();
            Response.Headers.ContentDisposition.ShouldNotBeNull();

            Response.Headers.ContentDisposition.Disposition.ShouldBe("attachment");
            Response.Headers.ContentDisposition.FileName.ShouldBe("test.txt");
        }

        [Test]
        public async Task a_downloadable_file_without_name_generates_a_content_disposition()
        {
            given_context();
            given_entity(new InMemoryDownloadableFile());

            await when_coding();
            Response.Headers.ContentDisposition.ShouldNotBeNull();

            Response.Headers.ContentDisposition.Disposition.ShouldBe("attachment");
            Response.Headers.ContentDisposition.FileName.ShouldBeNull();
        }

        async Task when_coding()
        {
            await CreateCodec(Context).WriteTo(_entity, Context.Response.Entity, null);
        }

        void given_entity(InMemoryFile file)
        {
            file.OpenStream().Write(Encoding.UTF8.GetBytes("Test data"));
            _entity = file;
        }

        protected override ApplicationOctetStreamCodec CreateCodec(ICommunicationContext context)
        {
            return new ApplicationOctetStreamCodec();
        }
    }

    [TestFixture]
    public class when_converting_a_byte_stream_to_an_instance_of_a_stream : applicationoctetstream_context
    {
        [Test]
        public async Task the_stream_length_is_set_to_the_size_of_the_sent_byte_stream()
        {
            given_context();
            given_request_entity_stream();
            given_content_disposition_header("attachment;filename=\"test.txt\"");

            await WhenParsing();

            ThenTheResult.Length.ShouldBe(1024);
        }

        public async Task WhenParsing()
        {
            await when_decoding<Stream>();
        }

        public Stream ThenTheResult => then_decoding_result<Stream>();
    }

    public class applicationoctetstream_context : media_type_reader_context<ApplicationOctetStreamCodec>
    {
        protected void given_request_entity_stream()
        {
            given_request_entity_stream(1024);
        }

        protected void given_request_entity_stream(int length)
        {
            given_request_stream(stream => stream.Write(new byte[length]));
        }

        protected override ApplicationOctetStreamCodec CreateCodec(ICommunicationContext context)
        {
            return new ApplicationOctetStreamCodec();
        }

        protected void given_content_disposition_header(string p)
        {
            Context.Request.Entity.Headers.ContentDisposition = new ContentDispositionHeader(p);
        }
    }
}

#region Full license

//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

#endregion