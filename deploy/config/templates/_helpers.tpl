{{/* vim: set filetype=mustache: */}}

{{/*
Return the proper image name
{{ include "common.images.image" ( dict "imageRoot" .Values.path.to.the.image "global" $) }}
*/}}
{{- define "app.images.image" -}}
{{- $registryName := .imageRoot.registry -}}
{{- $repositoryName := .imageRoot.repository -}}
{{- $tag := default .defaultTag .imageRoot.tag | toString -}}
{{- if .global }}
    {{- if .global.imageRegistry }}
     {{- $registryName = .global.imageRegistry -}}
    {{- end -}}
{{- end -}}
{{- if $registryName }}
{{- printf "%s/%s:%s" $registryName $repositoryName $tag -}}
{{- else -}}
{{- printf "%s:%s" $repositoryName $tag -}}
{{- end -}}
{{- end -}}

{{/*
Return the proper Service image name
*/}}
{{- define "app.image" -}}
{{ include "app.images.image" (dict "imageRoot" .Values.appImage "global" .Values.global "defaultTag" .Chart.AppVersion) }}
{{- end -}}

{{/*
Return the proper CLI image name
*/}}
{{- define "cli.image" -}}
{{ include "app.images.image" (dict "imageRoot" .Values.cliImage "global" .Values.global "defaultTag" .Chart.AppVersion) }}
{{- end -}}

{{/*
Return the proper curl image name
*/}}
{{- define "curl.image" -}}
{{ include "app.images.image" (dict "imageRoot" .Values.curlImage "global" .Values.global "defaultTag" .Chart.AppVersion) }}
{{- end -}}

{{/*
Return the proper postgre image name
*/}}
{{- define "postgre.image" -}}
{{ include "app.images.image" (dict "imageRoot" .Values.postgreImage "global" .Values.global "defaultTag" .Chart.AppVersion) }}
{{- end -}}

{{/*
Return the proper Docker Image Registry Secret Names
*/}}
{{- define "app.images.pullSecrets" -}}
{{- include "common.images.pullSecrets" (dict "images" (list .Values.appImage .Values.cliImage .Values.curlImage .Values.postgreImage) "global" .Values.global) -}}
{{- end -}}

{{/*
Create the name of the service account to use
*/}}
{{- define "app.serviceAccountName" -}}
{{- if .Values.serviceAccount.create -}}
    {{ default (include "common.names.fullname" .) .Values.serviceAccount.name }}
{{- else -}}
    {{ default "" .Values.serviceAccount.name }}
{{- end -}}
{{- end -}}

{{/*
Create a url from es connectionString to use in curl initContainer
*/}}
{{- define "app.eventstoreInitUrl" -}}
{{- $proto := "http://" -}}
{{- if (regexMatch "tls=true|usesslconnection=true" (.connectionString | lower)) -}}
{{- $proto = "https://" -}}
{{- end -}}
{{/* # set right protokoll */}}
{{- $uri := mustRegexReplaceAll "^([\\w\\+\\= ]+):\\/\\/" .connectionString $proto -}}
{{/* replace tcp port with http */}}
{{- $uri = mustRegexReplaceAll ":\\d+" $uri (default "2113" .httpPort | printf ":%s") -}}
{{/* remove trailing options */}}
{{- $uri = mustRegexReplaceAll ";.+" $uri "" -}}
{{- $uri -}}
{{- end -}}