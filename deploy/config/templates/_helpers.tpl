{{/* vim: set filetype=mustache: */}}
{{/*
Expand the name of the chart.
*/}}
{{- define "Config.name" -}}
{{- default .Chart.Name .Values.nameOverride | trunc 63 | trimSuffix "-" -}}
{{- end -}}

{{/*
Create a default fully qualified app name.
We truncate at 63 chars because some Kubernetes name fields are limited to this (by the DNS naming spec).
If release name contains chart name it will be used as a full name.
*/}}
{{- define "Config.fullname" -}}
{{- if .Values.fullnameOverride -}}
{{- .Values.fullnameOverride | trunc 63 | trimSuffix "-" -}}
{{- else -}}
{{- $name := default .Chart.Name .Values.nameOverride -}}
{{- if contains $name .Release.Name -}}
{{- .Release.Name | trunc 63 | trimSuffix "-" -}}
{{- else -}}
{{- printf "%s-%s" .Release.Name $name | trunc 63 | trimSuffix "-" -}}
{{- end -}}
{{- end -}}
{{- end -}}

{{/*
Create chart name and version as used by the chart label.
*/}}
{{- define "Config.chart" -}}
{{- printf "%s-%s" .Chart.Name .Chart.Version | replace "+" "_" | trunc 63 | trimSuffix "-" -}}
{{- end -}}

{{/*
Name of included eventstore host 
*/}}
{{- define "Config.eventstoreserver" -}}
{{- if .Values.eventstore.enabled -}}
{{- printf .Chart.Name -}}-eventstore
{{- else -}}
{{- .Values.eventstore.server -}}
{{- end -}}
{{- end -}}

{{/*
Name of included redis host 
*/}}
{{- define "Config.redisserver" -}}
{{- if .Values.redis.enabled -}}
{{- printf .Chart.Name -}}-redis-master
{{- else -}}
{{- .Values.redis.server -}}
{{- end -}}
{{- end -}}

{{/*
Name of included rabbitmq host 
*/}}
{{- define "Config.rabbitmqserver" -}}
{{- if .Values.rabbitmq.enabled -}}
{{- printf .Chart.Name -}}-rabbitmq
{{- else -}}
{{- .Values.rabbitmq.server -}}
{{- end -}}
{{- end -}}

{{/*
Name of included websocket host 
*/}}
{{- define "Config.websocketserver" -}}
{{- if .Values.websocket.enabled -}}
{{- printf .Chart.Name -}}-websocket
{{- else -}}
{{- .Values.websocket.server -}}
{{- end -}}
{{- end -}}

{{/*
Common labels
*/}}
{{- define "Config.labels" -}}
helm.sh/chart: {{ include "Config.chart" . }}
{{ include "Config.selectorLabelsService" . }}
{{- if .Chart.AppVersion }}
app.kubernetes.io/version: {{ .Chart.AppVersion | quote }}
{{- end }}
app.kubernetes.io/managed-by: {{ .Release.Service }}
{{- end -}}

{{/*
Selector labels
*/}}
{{- define "Config.selectorLabelsBase" -}}
app.kubernetes.io/name: {{ include "Config.name" . }}
app.kubernetes.io/instance: {{ .Release.Name }}
app: maverick
{{- end -}}

{{/*
Selector labels
*/}}
{{- define "Config.selectorLabelsService" -}}
{{ include "Config.selectorLabelsBase" . }}
app.kubernetes.io/type: Service
{{- end -}}

{{/*
Selector labels
*/}}
{{- define "Config.selectorLabelsWorker" -}}
{{ include "Config.selectorLabelsBase" . }}
app.kubernetes.io/type: Worker
{{- end -}}

{{/*
Create the name of the service account to use
*/}}
{{- define "Config.serviceAccountName" -}}
{{- if .Values.serviceAccount.create -}}
    {{ default (include "Config.fullname" .) .Values.serviceAccount.name }}
{{- else -}}
    {{ default "default" .Values.serviceAccount.name }}
{{- end -}}
{{- end -}}
