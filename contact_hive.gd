extends Button

var address = "localhost:3621"

# Called when the node enters the scene tree for the first time.
func _ready():
	pass # Replace with function body.

# Called every frame. 'delta' is the elapsed time since the previous frame.
func _process(delta):
	pass

func _on_line_edit_text_changed(new_text):
	address = new_text

func _on_pressed():
	$HTTPRequest.request("http://" + address + "/servers")

func array_to_string(arr: Array) -> String:
	var s = ""
	for i in arr:
		s += char(i)
	return s

func _on_request_completed(result, response_code, headers, body):
	
	print(array_to_string(body))
	
	#var json = JSON.parse_string(body.get_string_from_utf8())
	#print(json["url"])
