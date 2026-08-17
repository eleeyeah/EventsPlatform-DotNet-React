import {defineConfig} from 'vite'
import react, {reactCompilerPreset} from '@vitejs/plugin-react'
import babel from '@rolldown/plugin-babel';
import mkcert from 'vite-plugin-mkcert'


// https://vite.dev/config/
export default defineConfig({
	server: {
		port: 3000,
		proxy: {
			'/api': {
				target: 'https://localhost:5001',
				secure: false,
				changeOrigin: true,
			},
		}
	},
	plugins: [
		mkcert({
			hosts: ['localhost', '127.0.0.1', '::1'],
		}),
		react(),
		babel({presets: [reactCompilerPreset()]}),
	],
})
