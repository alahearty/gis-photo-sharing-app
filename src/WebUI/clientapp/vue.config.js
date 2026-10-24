// API when using `npm run serve`: match IIS Express (VS F5) or Kestrel (`dotnet run`).
const devApi =
  process.env.VUE_APP_DEV_API_TARGET ||
  "https://localhost:44387";

module.exports = {
  publicPath: process.env.NODE_ENV === "production" ? "/" : "/",
  outputDir: "dist",
  configureWebpack: {},
  devServer: {
    port: 8080,
    proxy: {
      "/api": {
        target: devApi,
        secure: false,
      },
      "/uploads": {
        target: devApi,
        secure: false,
      },
    },
  },
};