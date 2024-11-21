# PS5000A External Trigger Mod - QUICK & DIRTY Example!

As of 11/21/24:

Quick & Dirty really means that. Scrappy. Getting a feel for API low-hanging fruit and gotchas. 

Tests using a 3.3V logic level rising-edge signal connected to the external trigger input to trigger a one-time swept frequency output from the function generator.

Funtional test, check!

## This originated from PicoTech's Example

This is a minor modification of

[PicoTech's PS5000A Example here](https://github.com/picotech/picosdk-c-examples/tree/master/ps5000a)

See that link for the original source and the licensing there as well.

## Tested Environment:

MS Windows 10 Home

10.0.19045 Build 19045

x64-Based / PowerEdge T110 II

## Tested Hardware

PicoScope PS5244B

This 5000 series device uses the PS5000A dll / driver and thus the A API (see programmers manual).

More detail elsewhere if/as needed (private document right now).


## Tested SDK

PicoSDK_64_11.0.2.405

## Thanks github

The visual studio .gitignore comes from github's template for this at:

[https://github.com/github/gitignore/blob/main/VisualStudio.gitignore](https://github.com/github/gitignore/blob/main/VisualStudio.gitignore)

## Example Output Captured

![image](https://github.com/user-attachments/assets/b64217c7-5447-4a1f-b63e-bbbb4a1381cc)

