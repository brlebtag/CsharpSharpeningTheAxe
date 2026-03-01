# AppUriSingleApplication


This is a base flux needed to implement google authentication via a desktop app. The base folder structure is this:

* AppUriSingleApplication: Desktop app that when a button is clicked opens the default browser redirecting to a site. Only a single instances is allowed. The new instance that received the token sends to the previous instance waiting the token via named pipe before closing.
* TestServer: Site responsible to generate a Guid and redirect to the desktop app via Custom URI.
* WapAppUriSingleApplication: Installer of AppUriSingleApplication. It sets up the custom uri during instalation.