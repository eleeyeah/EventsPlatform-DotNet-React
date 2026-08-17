import {useEffect, useState} from "react";
import type {Event} from "./lib/types";
import {List, ListItem, ListItemText, Typography} from "@mui/material";
import axios from "axios";

function App() {
	
	const [events, setEvents] = useState<Event[]>([]);
	
	useEffect(()=> {
		axios.get<Event[]>('/api/events')
			.then(response => setEvents(response.data))
	}, [])
	
  return (
	  
      <>
	      <div>
		      <Typography variant='h3'>Events Platform</Typography>
		      <List>
			      {events.map((events) =>(
				      <ListItem key={events.id}>
					      <ListItemText>{events.title}</ListItemText>
				      </ListItem>
			      ))}
		      </List>
	      </div>
      </>
  )
}

export default App 