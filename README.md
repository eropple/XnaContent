XnaContent
==========

XnaContent is a wrapper that allows a developer to build XNA Content Pipeline
files (with an .xnb extension) from outside of Visual Studio. It supports any
non-parameterized importer/processor as you'd expect, and the wireup is fairly
simple and, perhaps more importantly, is typesafe; if Visual Studio can't find
your importer or processor, you aren't going to build your application.

While XnaContent works (I use it myself, this is an extraction from a personal
project), it does have some limitations. For example, if you need parameterized
content building, like for DXT textures, you'll need to manually specify the
importer. The good news is that the TypeMapping system (which uses regexes to
match file patterns to filenames) allows you to provide pretty expressive rules
and yours are checked before the built-ins so it's easy to override the default
"this is a PNG, build it as a Color texture" behavior.

It's important to note that the Build() method is embarrassingly synchronous.
This is intentional. I use it in an asynchronous manner, wrapped inside of a
Task, but I didn't want to force consumers to necessarily do the same (though
maybe I should have!).

PLEASE NOTE: at present, I have not yet written mappings for all of the XNA
types. If you'd like to provide a list of those mappings (even if you don't
want to code it, test it, and do a pull request), please drop me an email. I'll
get to it eventually otherwise, but I wanted to get this out there sooner.

Questions, comments, suggestions - feel free to email me or to file an issue
on Github.

-Ed Ropple

ed+xnacontent@edropple.com
