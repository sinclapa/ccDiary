#!/bin/sh
# Launches the API as an Azure Functions custom handler. The Functions host picks a free
# port per instance and passes it in FUNCTIONS_CUSTOMHANDLER_PORT, then proxies every
# request to it; Kestrel takes that port through the standard --urls switch, so nothing in
# the application itself knows it is running behind Functions.
cd "$(dirname "$0")"
exec ./ccDiaryApi --urls "http://127.0.0.1:${FUNCTIONS_CUSTOMHANDLER_PORT}"
