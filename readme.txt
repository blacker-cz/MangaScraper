MangaScraper

Description

MangaScraper is simple application which allows you to download manga chapters from online manga readers. Currently supported sites are:

* MangaStream.com
* MangaReader.net
* Batoto.net
* MangaFox.me
* EatManga.com
* EGScans.com
* Vortex-Scans Reader
* Red Hawk Scans Reader

If you want support for any other site, let me know. If you feel like implemening it, let me know too ;-)

Download and installation instructions

Latest version can be downloaded here - http://scraper.blacker.cz/. The latest folder should contain version based on the latest commit in repository. Starting with version 1.0 there are both installer and no-install package available for download.

Using installer

Using installer is straightforward, just start the installer and follow instructions in the install wizard. Installer will also make sure that all prerequisites are installed.

Using no-install package

Getting the no-install package up and running may be a bit tricky in some cases. Starting with Windows XP files downloaded from Interned are blocked by OS. You may have already encounter the following message "This file came from another computer and might be blocked to help protect this computer". So when you download the no-install package you need to unblock it before extracting its content to ensure that application can run properly. To unblock the file do the following:

  1. Right click on file and select Properties
  2. Under the General tab, click on the Unblock button
  3. Click on OK button

After unblocking the file you can safely extract it to wherever you need.

3rd party software, libraries, etc.

MangaScraper is using following 3rd-party libraries:

* Html Agility Pack - http://htmlagilitypack.codeplex.com/
* DotNetZip Library - http://dotnetzip.codeplex.com/
* log4net - http://logging.apache.org/log4net/
* MahApps.Metro - https://github.com/MahApps/MahApps.Metro
* System.Data.SQLite - http://system.data.sqlite.org/

these libraries are automatically loaded to the solution using NuGet (http://nuget.codeplex.com/)

MangaScraper is also using some of the icons from Silk icon set 1.3 (http://www.famfamfam.com/lab/icons/silk/), some icons from Templarian/WindowsIcons set (https://github.com/Templarian/WindowsIcons) and the new color scheme is loosely based on Metro pallete by COLOURlover (http://www.colourlovers.com/palette/1/metro)

Notes

This tool is intended for archiving of manga chapters from online readers, so you can read them even offline. However, most of these websites are up and running only thanks to advertisement and donations. And without these websites, this tool would be useless. So visit them or donate them some cash, just don't let them die. Because if they die, we will all lose our sources of manga scanlations.

License

MangaScraper
Copyright (c) 2012-2013, Lukáš Černý
All rights reserved.

Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following conditions are met:

* Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
* Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.