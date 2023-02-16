# Reddio

[![status](https://img.shields.io/uptimerobot/status/m791996229-c1866605aaf00d5bf74d505e)](https://stats.uptimerobot.com/4wvr6UzvYm)
[![uptime](https://img.shields.io/uptimerobot/ratio/m791996229-c1866605aaf00d5bf74d505e)](https://stats.uptimerobot.com/4wvr6UzvYm)
[![cd](https://img.shields.io/github/actions/workflow/status/yavorfingarov/Reddio/cd.yml?branch=master&label=cd)](https://github.com/yavorfingarov/Reddio/actions/workflows/cd.yml?query=branch%3Amaster)
[![codeql](https://img.shields.io/github/actions/workflow/status/yavorfingarov/Reddio/codeql.yml?branch=master&label=codeql)](https://github.com/yavorfingarov/Reddio/actions/workflows/codeql.yml?query=branch%3Amaster)
[![loc](https://img.shields.io/endpoint?url=https://gist.githubusercontent.com/yavorfingarov/d850286102a68e918ab12089f7497d60/raw/lines-of-code.json)](https://github.com/yavorfingarov/Reddio/actions/workflows/cd.yml?query=branch%3Amaster)
[![test coverage](https://img.shields.io/endpoint?url=https://gist.githubusercontent.com/yavorfingarov/d850286102a68e918ab12089f7497d60/raw/test-coverage.json)](https://github.com/yavorfingarov/Reddio/actions/workflows/cd.yml?query=branch%3Amaster)
[![mutation score](https://img.shields.io/endpoint?url=https://gist.githubusercontent.com/yavorfingarov/d850286102a68e918ab12089f7497d60/raw/mutation-score.json)](https://github.com/yavorfingarov/Reddio/actions/workflows/cd.yml?query=branch%3Amaster)

A small web application for listening to music from selected Reddit communities.

## Features

* Scheduled background job for fetching tracks from Reddit
* Client-side playback history
* GDPR-compliant cookie management
* Health check
* CD pipeline

## Tech stack

* ASP.NET Core 7 Razor Pages / Reprise
* SQLite / Dapper / DbUp
* NLog / SimpleRequestLogger
* Xunit / Moq / HtmlAgilityPack
