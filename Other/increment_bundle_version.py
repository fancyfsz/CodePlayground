import requests

TEAMCITY_URL = "https://your.teamcity.server"
PROJECT_ID = "_Root"
PARAM_NAME = "bundleVersionCode"
TOKEN = "YOUR_TOKEN_HERE"

headers = {
    "Authorization": f"Bearer {TOKEN}",
    "Content-Type": "text/plain"
}

def get_parameter_value():
    url = f"{TEAMCITY_URL}/app/rest/projects/id:{PROJECT_ID}/parameters/{PARAM_NAME}"
    r = requests.get(url, headers=headers)
    r.raise_for_status()
    return r.text.strip()

def set_parameter_value(value):
    url = f"{TEAMCITY_URL}/app/rest/projects/id:{PROJECT_ID}/parameters/{PARAM_NAME}"
    r = requests.put(url, headers=headers, data=str(value))
    r.raise_for_status()
    return r.status_code

def main():
    print("Fetching parameter:", PARAM_NAME)
    current_value_str = get_parameter_value()
    print("Current value:", current_value_str)

    try:
        current_value = int(current_value_str)
    except ValueError:
        print("Error: current parameter value is not an integer")
        return

    new_value = current_value + 1
    print("Updating value to:", new_value)

    set_parameter_value(new_value)
    print("Done")

if __name__ == "__main__":
    main()
