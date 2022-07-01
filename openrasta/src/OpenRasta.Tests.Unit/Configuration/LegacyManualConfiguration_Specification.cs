using System;
using System.Globalization;
using System.Linq;
using NUnit.Framework;
using OpenRasta.Codecs;
using OpenRasta.Configuration;
using OpenRasta.DI;
using OpenRasta.Handlers;
using OpenRasta.Tests.Unit.Configuration;
using OpenRasta.TypeSystem;
using OpenRasta.Web;
using Shouldly;

#pragma warning disable 612,618

namespace LegacyManualConfiguration_Specification
{
  public class when_adding_uris_to_a_resource : configuration_context
  {
    void ThenTheUriHasTheResource<TResource>(string uri, CultureInfo language, string name)
    {
      var match = Match(uri);
      match.ShouldNotBeNull();
      match.UriCulture.ShouldBe(language);
      match.ResourceKey.ShouldBe(TypeSystems.Default.FromClr(typeof(TResource)));
      match.UriName.ShouldBe( name);
    }

    UriRegistration Match(string uri)
    {
      return Resolver.Resolve<IUriResolver>().Match(new Uri(new Uri("http://localhost/", UriKind.Absolute), uri));
    }

    [Test]
    public void language_and_names_are_properly_registered()
    {
      GivenAResourceRegistrationFor<Customer>("/customer",uri=>
        uri.InLanguage("fr").Named("French"));

      WhenTheConfigurationIsFinished();

      ThenTheUriHasTheResource<Customer>("/customer", CultureInfo.GetCultureInfo("fr"), "French");
    }

    [Test]
    public void equivalent_uris_are_parsed()
    {
      GivenAResourceRegistrationFor<Customer>("/customer/{id}",uri=>
          uri.And.AtUri("/customer/3"));

      WhenTheConfigurationIsFinished();

      Match("/customer/1").UriTemplateParameters.Count().ShouldBe(1);

      var equivalent = Match("/customer/3");
      equivalent.UriTemplate.ShouldBe("/customer/3");
      
      // legacy implementation
      equivalent.UriTemplateParameters.Count().ShouldBe(2);
      
      equivalent.Results.Count().ShouldBe(2);
    }
    [Test]
    public void registering_two_urls_works()
    {
      GivenAResourceRegistrationFor<Customer>("/customer/{id}",uri=>uri
        .InLanguage("en-CA")
        .AndAt("/privileged/customer/{id}")
        .Named("Privileged"));

      WhenTheConfigurationIsFinished();

      ThenTheUriHasTheResource<Customer>("/customer/{id}", CultureInfo.GetCultureInfo("en-CA"), null);
      ThenTheUriHasTheResource<Customer>("/privileged/customer/{id}", null, "Privileged");
    }

    [Test]
    public void the_base_uri_is_registered_for_that_resource()
    {
      GivenAResourceRegistrationFor<Customer>("/customer");

      WhenTheConfigurationIsFinished();

      ThenTheUriHasTheResource<Customer>("/customer", null, null);
    }
  }

  public class when_configuring_codecs : configuration_context
  {
    CodecRegistration ThenTheCodecFor<TResource, TCodec>(string mediaType)
    {
      return
        Resolver.Resolve<ICodecRepository>()
          .Where(codec => codec.ResourceType.CompareTo(TypeSystems.Default.FromClr(typeof(TResource))) == 0 &&
                          codec.CodecType == typeof(TCodec) && codec.MediaType.MediaType == mediaType)
          .Distinct()
          .SingleOrDefault();
    }

    [MediaType("application/vnd.rasta.test")]
    class Codec : NakedCodec
    {
    }

    [MediaType("application/vnd.rasta.test1")]
    [MediaType("application/vnd.rasta.test2")]
    class MultiCodec : NakedCodec
    {
    }

    class NakedCodec : ICodec
    {
      public object Configuration { get; set; }
    }

    [Test]
    public void a_codec_registered_with_configuration_media_type_doesnt_have_the_attribute_media_type_registered()
    {
      GivenAResourceRegistrationFor<Customer>("/customer",uri=>uri
        .HandledBy<CustomerHandler>()
        .AndTranscodedBy<Codec>()
        .ForMediaType("application/vnd.rasta.custom"));

      WhenTheConfigurationIsFinished();

      ThenTheCodecFor<Customer, Codec>("application/vnd.rasta.test").ShouldBeNull();
      ThenTheCodecFor<Customer, Codec>("application/vnd.rasta.custom").ShouldNotBeNull();
    }

    [Test]
    public void a_codec_registered_with_two_media_type_attributes_is_registered_twice()
    {
      GivenAResourceRegistrationFor<Customer>("/customer",uri=>uri
        .HandledBy<CustomerHandler>()
        .AndTranscodedBy<MultiCodec>());

      WhenTheConfigurationIsFinished();

      ThenTheCodecFor<Customer, MultiCodec>("application/vnd.rasta.test1").ShouldNotBeNull();
      ThenTheCodecFor<Customer, MultiCodec>("application/vnd.rasta.test2").ShouldNotBeNull();
    }

    [Test]
    public void a_codec_registered_with_two_media_types_in_configuration_is_registered_twice()
    {
      GivenAResourceRegistrationFor<Customer>("/customer",uri=>uri
        .HandledBy<CustomerHandler>()
        .AndTranscodedBy<Codec>()
        .ForMediaType("application/vnd.rasta.config1")
        .AndForMediaType("application/vnd.rasta.config2"));

      WhenTheConfigurationIsFinished();

      ThenTheCodecFor<Customer, Codec>("application/vnd.rasta.config1").ShouldNotBeNull();
      ThenTheCodecFor<Customer, Codec>("application/vnd.rasta.config2").ShouldNotBeNull();
    }

    [Test]
    public void a_codec_registered_without_media_types_is_registered_with_the_default_attributed_media_types()
    {
      GivenAResourceRegistrationFor<Customer>("/customer",uri=>uri
        .HandledBy<CustomerHandler>()
        .AndTranscodedBy<Codec>());

      WhenTheConfigurationIsFinished();

      ThenTheCodecFor<Customer, Codec>("application/vnd.rasta.test").ShouldNotBeNull();
    }

    [Test]
    public void registering_a_codec_without_media_type_in_config_or_in_attributes_raises_an_error()
    {
      GivenAResourceRegistrationFor<Customer>("/customer",uri=>uri
        .HandledBy<CustomerHandler>()
        .AndTranscodedBy<NakedCodec>());

      Executing((Action) WhenTheConfigurationIsFinished)
        .ShouldThrow<OpenRastaConfigurationException>();
    }
  }

  public class when_adding_handlers : configuration_context
  {
    IType ThenTheUriHasTheHandler<THandler>(string uri)
    {
      var urimatch = Resolver.Resolve<IUriResolver>().Match(new Uri(new Uri("http://localhost/", UriKind.Absolute), uri));
      urimatch.ShouldNotBeNull();

      var handlerMatch = Resolver.Resolve<IHandlerRepository>().GetHandlerTypesFor(urimatch.ResourceKey).FirstOrDefault();
      handlerMatch.ShouldNotBeNull();
      handlerMatch.ShouldBe(TypeSystems.Default.FromClr(typeof(THandler)));
      return handlerMatch;
    }

    [Test]
    public void the_handler_is_registered()
    {
      GivenAResourceRegistrationFor<Customer>("/customer",uri=>uri
        .HandledBy<CustomerHandler>());

      WhenTheConfigurationIsFinished();

      ThenTheUriHasTheHandler<CustomerHandler>("/customer");
    }
  }
}

#pragma warning restore 612,618
