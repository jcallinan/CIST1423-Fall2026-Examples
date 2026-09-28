/* eslint-env node */
const path = require('path');
const HtmlWebpackPlugin = require('html-webpack-plugin');
const CopyPlugin = require('copy-webpack-plugin');
const MiniCssExtractPlugin = require('mini-css-extract-plugin');

module.exports = {
	mode: 'development',
	entry: {
		index: './src/index.js',
	},
	module: {
		rules: [
			{
				test: /\.m?js$/,
				resolve: {
					fullySpecified: false,
				},
			},
			{
				test: /\.css$/i,
				use: [MiniCssExtractPlugin.loader, 'css-loader'],
			},
		],
	},
	devServer: {
		static: {
			directory: path.join(__dirname, 'dist'),
		},
		host: '0.0.0.0',
		server: 'https',
		compress: true,
		port: 8081,
		client: {
			overlay: { warnings: false, errors: true },
		},
	},
	output: {
		filename: '[name].bundle.js',
		path: path.resolve(__dirname, 'dist'),
		clean: true,
	},
	plugins: [
		new HtmlWebpackPlugin({
			template: './src/index.html',
		}),
		new MiniCssExtractPlugin({
			filename: 'styles/[name].css',
		}),
		new CopyPlugin({
			patterns: [
				{ from: 'src/assets', to: 'assets' },
				{
					from: 'node_modules/three/examples/jsm/libs/basis',
					to: 'vendor/basis',
				},
				{
					from: 'node_modules/three/examples/jsm/libs/draco',
					to: 'vendor/draco',
				},
			],
		}),
	],
	devtool: 'source-map',
};
