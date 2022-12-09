using Contracts;
using ProtoBuf.Grpc.Reflection;
using ProtoBuf.Meta;

SchemaGenerator generator = new SchemaGenerator { ProtoSyntax = ProtoSyntax.Proto3 };
var calculator = generator.GetSchema<ICalculator>();
Console.WriteLine(calculator);
File.WriteAllText("../../../../PureGrpcClientConsole/calculator.proto", calculator);

var timeService = generator.GetSchema<ITimeService>();
Console.WriteLine(timeService);
File.WriteAllText("../../../../PureGrpcClientConsole/timeService.proto", timeService);

