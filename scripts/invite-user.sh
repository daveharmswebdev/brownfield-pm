#!/bin/bash

# invite-user.sh - Send an invitation to a new user
# Usage: ./scripts/invite-user.sh user@example.com

set -e

API_URL="${API_URL:-http://localhost:5292/api/v1}"
EMAIL="$1"

if [ -z "$EMAIL" ]; then
  echo "Usage: $0 <email>"
  echo "Example: $0 friend@example.com"
  exit 1
fi

# Prompt for credentials
read -p "Your email: " OWNER_EMAIL
read -s -p "Your password: " OWNER_PASSWORD
echo

# Step 1: Login to get access token
echo "Logging in..."
LOGIN_RESPONSE=$(curl -s -X POST "$API_URL/auth/login" \
  -H "Content-Type: application/json" \
  -d "{\"email\": \"$OWNER_EMAIL\", \"password\": \"$OWNER_PASSWORD\"}")

ACCESS_TOKEN=$(echo "$LOGIN_RESPONSE" | jq -r '.accessToken')

if [ "$ACCESS_TOKEN" == "null" ] || [ -z "$ACCESS_TOKEN" ]; then
  echo "Login failed. Response:"
  echo "$LOGIN_RESPONSE"
  exit 1
fi

echo "Login successful."

# Step 2: Send invitation
echo "Sending invitation to $EMAIL..."
INVITE_RESPONSE=$(curl -s -X POST "$API_URL/auth/invite" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $ACCESS_TOKEN" \
  -d "{\"email\": \"$EMAIL\"}")

echo "Response:"
echo "$INVITE_RESPONSE" | jq .

echo "Done! Check MailHog at http://localhost:8025 for the invitation email."
