#!/bin/bash
pid=$1
app_path=$2

# Wait until the old process exits
while kill -0 ""$pid"" 2>/dev/null; do sleep 1; done

# Start the new instance
dotnet ""$app_path"" &