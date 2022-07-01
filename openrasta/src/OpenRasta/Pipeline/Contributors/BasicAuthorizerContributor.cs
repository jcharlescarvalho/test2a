#region License

/* Authors:
 *      Dylan Beattie (dylan@dylanbeattie.net)
 *      Sebastien Lambla (seb@serialseb.com)
 * Copyright:
 *      (C) 2007-2015 Caffeine IT & naughtyProd Ltd (http://www.caffeine-it.com)
 * License:
 *      This file is distributed under the terms of the MIT License found at the end of this file.
 */

#endregion

// HTTP Basic Authentication implementation 
// Adapts approach used in https://github.com/scottlittlewood/OpenRastaAuthSample to support pipeline contributor model.

using System;
using System.Linq;
using System.Security.Principal;
using OpenRasta.DI;
using OpenRasta.Security;
using OpenRasta.Web;
using OpenRasta.Pipeline;

namespace OpenRasta.Pipeline.Contributors
{
    public class BasicAuthorizerContributor : IPipelineContributor
    {
        private readonly IDependencyResolver _resolver;
        private IAuthenticationProvider _authentication;

        public BasicAuthorizerContributor(IDependencyResolver resolver)
        {
            _resolver = resolver;
        }

        public void Initialize(IPipeline pipelineRunner)
        {
            _authentication = _resolver.Resolve<IAuthenticationProvider>();
            pipelineRunner.Notify(ReadCredentials)
                .After<KnownStages.IBegin>()
                .And
                .Before<KnownStages.IHandlerSelection>();
        }

        public PipelineContinuation ReadCredentials(ICommunicationContext context)
        {
            if (_resolver.HasDependency(typeof(IAuthenticationProvider)))
            {
                _authentication = _resolver.Resolve<IAuthenticationProvider>();
                var header = ReadBasicAuthHeader(context);
                if (header != null)
                {
                    var credentials = _authentication.GetByUsername(header.Username);

                    if (_authentication.ValidatePassword(credentials, header.Password))
                    {
                        IIdentity id = new GenericIdentity(credentials.Username, "Basic");
                        context.User = new GenericPrincipal(id, credentials.Roles);
                    }
                }
            }

            return PipelineContinuation.Continue;
        }

        private static BasicAuthorizationHeader ReadBasicAuthHeader(ICommunicationContext context)
        {
            try
            {
                var header = context.Request.Headers["Authorization"];
                return string.IsNullOrEmpty(header) ? null : BasicAuthorizationHeader.Parse(header);
            } catch (ArgumentException ex)
            {
                return (null);
            }
        }
    }
}

#region Full license

// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

#endregion
