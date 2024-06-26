# JSHint, A Static Code Analysis Tool for JavaScript

\[ [Use it online](http://jshint.com/) •
[Docs](http://jshint.com/docs/) • [FAQ](http://jshint.com/docs/faq) •
[Install](http://jshint.com/install/) •
[Contribute](http://jshint.com/contribute/) •
[Blog](http://jshint.com/blog/) • [Twitter](https://twitter.com/jshint/) \]

[![NPM version](https://img.shields.io/npm/v/jshint.svg?style=flat)](https://www.npmjs.com/package/jshint)
[![Linux Build Status](https://img.shields.io/travis/jshint/jshint/master.svg?style=flat&label=Linux%20build)](https://travis-ci.org/jshint/jshint)
[![Windows Build status](https://img.shields.io/appveyor/ci/jshint/jshint/master.svg?style=flat&label=Windows%20build)](https://ci.appveyor.com/project/jshint/jshint/branch/master)
[![Coverage Status](https://img.shields.io/coveralls/jshint/jshint.svg?style=flat)](https://coveralls.io/r/jshint/jshint?branch=master)

JSHint is a community-driven tool that detects errors and potential problems in
JavaScript code. Since JSHint is so flexible, you can easily adjust it in 
the environment you expect your code to execute. JSHint is publicly available
and will always stay this way.

## Our goal

The project aims to help JavaScript developers write complex programs
without worrying about typos and language gotchas.

Any code base eventually becomes huge at some point, so simple mistakes — that
would not show themselves when written — can become show stoppers and add 
extra hours of debugging. So, static code analysis tools come into play
and help developers spot such problems. JSHint scans a program written in
JavaScript and reports about commonly made mistakes and potential bugs. The
potential problem could be a syntax error, a bug due to an implicit type
conversion, a leaking variable, or something else entirely.

Only 15% of all programs linted on [jshint.com](http://jshint.com) pass the
JSHint checks. In all other cases, JSHint finds some red flags that could've
been bugs or potential problems.

Please note, that while static code analysis tools can spot many different kind
of mistakes, it can't detect if your program is correct, fast or has memory
leaks. You should always combine tools like JSHint with unit and functional
tests as well as with code reviews.

## Reporting a bug

To report a bug simply create a
[new GitHub Issue](https://github.com/jshint/jshint/issues/new) and describe
your problem or suggestion. We welcome all kinds of feedback regarding
JSHint including but not limited to:

 * When JSHint doesn't work as expected
 * When JSHint complains about valid JavaScript code that works in all browsers
 * When you simply want a new option or feature

Before reporting a bug, please look around to see if there are any open or closed tickets
that discuss your issue, and remember the wisdom: pull request > bug report > tweet.

## Who uses JSHint?

Engineers from these companies and projects use JSHint:

* [Mozilla](https://www.mozilla.org/)
* [Wikipedia](https://wikipedia.org/)
* [Facebook](https://facebook.com/)
* [Twitter](https://twitter.com/)
* [Disqus](https://disqus.com/)
* [Medium](https://medium.com/)
* [Yahoo!](https://yahoo.com/)
* [SmugMug](http://smugmug.com/)
* [jQuery UI](https://jqueryui.com/) ([Source](https://github.com/jquery/jquery-ui/blob/master/package.json))
* [jQuery Mobile](https://jquerymobile.com/) ([Source](https://github.com/jquery/jquery-mobile/blob/master/package.json#))
* [Coursera](http://coursera.com/)
* [RedHat](http://redhat.com/)
* [SoundCloud](http://soundcloud.com/)
* [Nodejitsu](http://nodejitsu.com/)
* [Yelp](https://yelp.com/)
* [Find My Electric](https://www.findmyelectric.com/)
* [Voxer](http://voxer.com/)
* [EnyoJS](http://enyojs.com/)
* [QuickenLoans](http://quickenloans.com/)
* [Cloud9](http://c9.io/)
* [CodeClimate](https://codeclimate.com/)
* [Zendesk](http://zendesk.com/)
* [Google](https://www.google.com/)
* [Codacy](https://www.codacy.com) [ref](https://support.codacy.com/hc/en-us/articles/207995005-Special-Thanks)
* [Spotify](https://www.spotify.com)

And many more!

## License

JSHint is licensed under [the MIT Expat
license](https://www.gnu.org/licenses/license-list.html#Expat).

Prior to version 2.12.0 (release in August 2020), JSHint was partially licensed
under the non-free [JSON
license](https://www.gnu.org/licenses/license-list.html#JSON). [The 2020
Relicensing document](http://jshint.com/relicensing-2020/) details the process
maintainers followed to change the license.

## The JSHint Team

JSHint is currently maintained by [Rick Waldron](https://github.com/rwaldron/),
[Caitlin Potter](https://github.com/caitp/), [Mike
Pennisi](https://github.com/jugglinmike/), and [Luke
Page](https://github.com/lukeapage). You can reach them via admin@jshint.org.

## Previous Maintainers

Originating from the JSLint project in 2010, JSHint has been maintained by a
number of dedicated individuals. In chronological order, they are: Douglas
Crockford, Anton Kovalyov, and Mike Sherov. We appreciate their long-term
commitment!

## Thank you!

We really appreciate all kinds of feedback and contributions. Thanks for using and supporting JSHint!
