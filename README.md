# Zendesk Support API

Zendesk Support API is provided by Zendesk. See https://developer.zendesk.com/rest_api/docs/support/introduction

This package contains limited features of Zendesk Support API, implemented in F#. The initial goal for this package is to support features that my Zendesk - Severa Integration will need.

## About Zendesk Support API

Zendesk's PUT works like PATCH. When the request does not contain the field then it will not be affected. This feature is not used at the moment.

When you delete an entity that does not exist (or has already been deleted) then the response is Not found (404). For this reason the API does treat this situation as success.

Zendesk's models contain an infrastructure model around the actual model. You can see this in the models (see XxxxxxTypes.fs files). In the program you will use the actual model.

The API has at least two models for each entity. The normal one contains all fields that the entity returns from the Zendesk. That type is used in PUT, even though there are usually fields that the Zendesk will ignore as they are read-only. For POST the API contains NewXxxxx type that only includes fields that you can control.

The Zendesk.getArray function operates on PagedModel types and will request all entities in a recursive loop until the last page has been retrieved. If you use Zendesk.get for PagedModel then paging is not used.

Http module has multiple functions to handle send. You can compose them to form a send function which you want to use.

Each business entity related module contains functions to compose a command in a declarative way. That command is used as a parameter to methods in Zendesk module.

Each function uses a railway-oriented programming pattern, and in practice each function returns a Result<'a, Failure> type.

## Using the API

<pre>
let context =
    {
        BaseUrl = Uri("https://???.zendesk.com", UriKind.Absolute)
        Credentials = UsernameToken {
            Username = "???@???.???"
            Token = "???"
        }
    }

let result = Zendesk.getArray Http.send Organization.getAll context
</pre>

------

Copyright (c) 2020 Jarmo Muukka, Mutex Oy
