# builds both the TestThreading project and the Threading project.

echo "" > build.log

mkdir -p /usr/local/lib/JAMSolutions/Apps/

cd TestThreading
./build.sh
cd ..

cat build.log | grep error

