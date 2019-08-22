EXPECTED="{\"category\":\"av360\",\"name\":\"dev\"}"
RESP=$(curl -X GET "http://configservice:8082/v1/environments/available?offset=-1&length=-1" | jq -r -c '.[] | select(.category == "av360") | select(.name == "dev")')
echo resp: "$RESP"
echo expc: "$EXPECTED"
if [ "$RESP" = "$EXPECTED" ]; then
echo env exists
else
exit 1
fi