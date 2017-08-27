import * as gulp from "gulp";
import * as BuildKit from "@spicypixel/build-kit-js";
import * as UnityKit from "@spicypixel/unity-kit-js";
import * as CoreKit from "@spicypixel/core-kit-js";
import FileSystem = CoreKit.FileSystem;

const monoDocBuilder = new BuildKit.MonoDocBuilder();

gulp.task("default", () => build());
gulp.task("clean", () => clean());
gulp.task("rebuild", () => rebuild());
gulp.task("build:code", () => buildCode());
gulp.task("build:docs", () => buildDocs());
gulp.task("build", () => build());
gulp.task("test", () => test());
gulp.task("install:monodocs", () => installMonoDocs());

async function clean() {
  await FileSystem.removePatternsAsync([
    "Source/**/bin/*", "Source/**/obj/*",
    "Doxygen/html", "Doxygen/xml", "Doxygen/qt",
    "MonoDoc/xml", "MonoDoc/assemble", "MonoDoc/html",
    "Source/packages/*/"]);
}

async function rebuild() {
  await clean();
  await build();
}

async function build() {
  await buildCode();
  await buildDocs();
}

async function buildCode() {
  await BuildKit.MSBuildBuilder.restoreAsync("Source");
  await BuildKit.MSBuildBuilder.buildAsync("Source/*.sln", {
    properties: {
      UnityEnginePath: UnityKit.UnityEditor.enginePath
    },
    targets: ["Build"],
  });
}

async function installMonoDocs() {
  await monoDocBuilder.installAsync();
}

async function buildDocs() {
  await BuildKit.DoxygenBuilder.buildAsync();

  await monoDocBuilder.buildAsync("SpicyPixel.ConcurrencyKit",
  [
    "System.Threading",
    "SpicyPixel.Threading",
    "SpicyPixel.Threading.Unity"
  ]);
}

async function test() {
  await buildCode();
  await BuildKit.NUnitRunner.runAsync();
}