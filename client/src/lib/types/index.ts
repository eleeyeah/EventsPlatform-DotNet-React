export type Event = {
	id: string
	title: string
	date: string
	isCancelled: boolean
	description: string | null
	category: string | null
	city: string
	venue: string
	latitude: number
	longitude: number
}
