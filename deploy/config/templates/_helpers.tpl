{{/* vim: set filetype=mustache: */}}

{{/*
Fully qualified app name for PostgreSQL
*/}}
{{- define "config.fullname" -}}
{{- if .Values.fullnameOverride -}}
{{- printf "%s" .Values.fullnameOverride | trunc 63 | trimSuffix "-" -}}
{{- else -}}
{{- $name := default .Chart.Name .Values.nameOverride -}}
{{- if contains $name .Release.Name -}}
{{- printf "%s" .Release.Name | trunc 63 | trimSuffix "-" -}}
{{- else -}}
{{- printf "%s-%s" .Release.Name $name | trunc 63 | trimSuffix "-" -}}
{{- end -}}
{{- end -}}
{{- end -}}

{{/*
Return the proper image name
{{ include "common.images.image" ( dict "imageRoot" .Values.path.to.the.image "global" $) }}
*/}}
{{- define "config.images.image" -}}
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
{{- define "config.image" -}}
{{ include "config.images.image" (dict "imageRoot" .Values.configImage "global" .Values.global "defaultTag" .Chart.AppVersion) }}
{{- end -}}

{{/*
Return the proper CLI image name
*/}}
{{- define "cli.image" -}}
{{ include "config.images.image" (dict "imageRoot" .Values.cliImage "global" .Values.global "defaultTag" .Chart.AppVersion) }}
{{- end -}}

{{/*
Return the proper curl image name
*/}}
{{- define "curl.image" -}}
{{ include "config.images.image" (dict "imageRoot" .Values.curlImage "global" .Values.global "defaultTag" .Chart.AppVersion) }}
{{- end -}}

{{/*
Return the proper postgre image name
*/}}
{{- define "postgre.image" -}}
{{ include "config.images.image" (dict "imageRoot" .Values.postgreImage "global" .Values.global "defaultTag" .Chart.AppVersion) }}
{{- end -}}

{{/*
Return the proper Docker Image Registry Secret Names
*/}}
{{- define "config.imagePullSecrets" -}}
{{- include "common.images.pullSecrets" (dict "images" (list .Values.configImage .Values.cliImage .Values.curlImage .Values.postgreImage) "global" .Values.global) -}}
{{- end -}}

{{/*
Create chart name and version as used by the chart label.
*/}}
{{- define "Config.chart" -}}
{{- printf "%s-%s" .Chart.Name .Chart.Version | replace "+" "_" | trunc 63 | trimSuffix "-" -}}
{{- end -}}

{{/*
Common labels
*/}}
{{- define "config.labels.standard" -}}
{{ include "common.labels.standard" . }}
app.kubernetes.io/component: config
{{- end -}}

{{/*
match labels
*/}}
{{- define "config.labels.matchLabels" -}}
{{ include "common.labels.matchLabels" . }}
app.kubernetes.io/component: config
{{- end -}}