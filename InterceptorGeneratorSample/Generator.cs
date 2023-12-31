﻿using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace InterceptorGeneratorSample;

[Generator(LanguageNames.CSharp)]
public class Generator :
    IIncrementalGenerator
{
    public void Initialize(
        IncrementalGeneratorInitializationContext context)
    {
        var source = context.SyntaxProvider
            .CreateSyntaxProvider(
                static (node, cancellationToken) =>
                    node is IdentifierNameSyntax
                    {
                        Identifier.Text: nameof(Console.WriteLine),
                        Parent: MemberAccessExpressionSyntax
                        {
                            Expression: IdentifierNameSyntax,
                            Parent: InvocationExpressionSyntax
                            {
                                ArgumentList.Arguments:
                                [
                                    {
                                        Expression: LiteralExpressionSyntax argument
                                    }
                                ]
                            }
                        } memberAccess
                    }
                    && memberAccess.IsKind(SyntaxKind.SimpleMemberAccessExpression)
                    && argument.IsKind(SyntaxKind.StringLiteralExpression),
                static (context, cancellationToken) =>
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var node = (IdentifierNameSyntax)context.Node;
                    var symbolInfo = context.SemanticModel.GetSymbolInfo(node, cancellationToken);
                    var symbol = (IMethodSymbol)symbolInfo.Symbol!;

                    return (Node: node, Symbol: symbol);
                })
            .Collect()
            .Combine(context.CompilationProvider);

        context.RegisterImplementationSourceOutput(
            source,
            static (context, source) =>
            {
                var nodes = source.Left;
                var compilation = source.Right;

                var consoleSymbol = compilation.GetTypeByMetadataName("System.Console");

                var attributeList = new List<AttributeSyntax>();

                foreach (var (node, symbol) in nodes)
                {
                    context.CancellationToken.ThrowIfCancellationRequested();

                    if (!SymbolEqualityComparer.Default.Equals(symbol.ReceiverType, consoleSymbol))
                    {
                        continue;
                    }

                    var path = GetInterceptorFilePath(node.SyntaxTree, compilation);
                    var pos = node.GetLocation().GetLineSpan().StartLinePosition;

                    var attribute = SyntaxFactory
                        .Attribute(
                            SyntaxFactory.ParseName("global::System.Runtime.CompilerServices.InterceptsLocationAttribute"),
                            SyntaxFactory.AttributeArgumentList(
                                SyntaxFactory.SeparatedList(
                                    new[]
                                    {
                                        SyntaxFactory.AttributeArgument(
                                            default,
                                            default,
                                            SyntaxFactory.LiteralExpression(
                                                SyntaxKind.StringLiteralExpression,
                                                SyntaxFactory.Literal(path))),
                                        SyntaxFactory.AttributeArgument(
                                            default,
                                            default,
                                            SyntaxFactory.LiteralExpression(
                                                SyntaxKind.NumericLiteralExpression,
                                                SyntaxFactory.Literal(pos.Line + 1))),
                                        SyntaxFactory.AttributeArgument(
                                            default,
                                            default,
                                            SyntaxFactory.LiteralExpression(
                                                SyntaxKind.NumericLiteralExpression,
                                                SyntaxFactory.Literal(pos.Character + 1)))
                                    })));

                    attributeList.Add(attribute);
                }

                var compilationUnit = SyntaxFactory
                    .CompilationUnit()
                    .WithMembers(
                        SyntaxFactory.List(
                            new MemberDeclarationSyntax[]
                            {
                                SyntaxFactory
                                    .NamespaceDeclaration(
                                        SyntaxFactory.ParseName("System.Runtime.CompilerServices"))
                                    .WithMembers(
                                        SyntaxFactory.List(
                                            new MemberDeclarationSyntax[]
                                            {
                                                SyntaxFactory
                                                    .ClassDeclaration("InterceptsLocationAttribute")
                                                    .WithModifiers(
                                                        SyntaxFactory.TokenList(
                                                            SyntaxFactory.Token(SyntaxKind.FileKeyword),
                                                            SyntaxFactory.Token(SyntaxKind.SealedKeyword)))
                                                    .WithAttributeLists(
                                                        SyntaxFactory.List(
                                                            new AttributeListSyntax[]
                                                            {
                                                                SyntaxFactory.AttributeList(
                                                                    SyntaxFactory.SingletonSeparatedList(
                                                                        SyntaxFactory.Attribute(
                                                                            SyntaxFactory.ParseName("global::System.Diagnostics.ConditionalAttribute"),
                                                                            SyntaxFactory.AttributeArgumentList(
                                                                                SyntaxFactory.SingletonSeparatedList(
                                                                                        SyntaxFactory.AttributeArgument(
                                                                                            default,
                                                                                            default,
                                                                                            SyntaxFactory.LiteralExpression(
                                                                                                SyntaxKind.StringLiteralExpression,
                                                                                                SyntaxFactory.Literal("GENERATOR_ONLY")))))))),
                                                                SyntaxFactory.AttributeList(
                                                                    SyntaxFactory.SingletonSeparatedList(
                                                                        SyntaxFactory.Attribute(
                                                                            SyntaxFactory.ParseName("global::System.AttributeUsageAttribute"),
                                                                            SyntaxFactory.AttributeArgumentList(
                                                                                SyntaxFactory.SeparatedList(
                                                                                    new[]
                                                                                    {
                                                                                        SyntaxFactory.AttributeArgument(
                                                                                            default,
                                                                                            default,
                                                                                            SyntaxFactory.MemberAccessExpression(
                                                                                                SyntaxKind.SimpleMemberAccessExpression,
                                                                                                SyntaxFactory.ParseTypeName("global::System.AttributeTargets"),
                                                                                                SyntaxFactory.IdentifierName(nameof(AttributeTargets.Method)))),
                                                                                        SyntaxFactory.AttributeArgument(
                                                                                            SyntaxFactory.NameEquals(
                                                                                                SyntaxFactory.IdentifierName("AllowMultiple")),
                                                                                            default,
                                                                                            SyntaxFactory.LiteralExpression(
                                                                                                SyntaxKind.TrueLiteralExpression))
                                                                                    })))))
                                                            }))
                                                    .WithBaseList(
                                                        SyntaxFactory.BaseList(
                                                            SyntaxFactory.SingletonSeparatedList<BaseTypeSyntax>(
                                                                SyntaxFactory.SimpleBaseType(
                                                                    SyntaxFactory.ParseTypeName("global::System.Attribute")))))
                                                    .WithMembers(
                                                        SyntaxFactory.SingletonList<MemberDeclarationSyntax>(
                                                            SyntaxFactory
                                                                .ConstructorDeclaration("InterceptsLocationAttribute")
                                                                .WithModifiers(
                                                                    SyntaxFactory.TokenList(
                                                                        SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
                                                                .WithParameterList(
                                                                    SyntaxFactory.ParameterList(
                                                                        SyntaxFactory.SeparatedList(
                                                                            new[]
                                                                            {
                                                                                SyntaxFactory
                                                                                    .Parameter(SyntaxFactory.Identifier("filePath"))
                                                                                    .WithType(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.StringKeyword))),
                                                                                SyntaxFactory
                                                                                    .Parameter(SyntaxFactory.Identifier("line"))
                                                                                    .WithType(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.IntKeyword))),
                                                                                SyntaxFactory.Parameter(SyntaxFactory.Identifier("character"))
                                                                                    .WithType(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.IntKeyword)))
                                                                            })))
                                                                .WithBody(
                                                                    SyntaxFactory.Block())))
                                            })),
                                SyntaxFactory
                                    .ClassDeclaration("Interceptor")
                                    .WithModifiers(
                                        SyntaxFactory.TokenList(
                                            SyntaxFactory.Token(SyntaxKind.FileKeyword),
                                            SyntaxFactory.Token(SyntaxKind.StaticKeyword)))
                                    .WithMembers(
                                        SyntaxFactory.SingletonList<MemberDeclarationSyntax>(
                                            SyntaxFactory
                                                .MethodDeclaration(
                                                    SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)),
                                                    "Intercept")
                                                .WithModifiers(
                                                    SyntaxFactory.TokenList(
                                                        SyntaxFactory.Token(SyntaxKind.PublicKeyword),
                                                        SyntaxFactory.Token(SyntaxKind.StaticKeyword)))
                                                .WithParameterList(
                                                    SyntaxFactory.ParameterList(
                                                        SyntaxFactory.SingletonSeparatedList(
                                                            SyntaxFactory
                                                                .Parameter(SyntaxFactory.Identifier("value"))
                                                                .WithType(
                                                                    SyntaxFactory.NullableType(
                                                                        SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.StringKeyword)))))))
                                                .WithBody(
                                                    SyntaxFactory.Block(
                                                        SyntaxFactory.SingletonList<StatementSyntax>(
                                                            SyntaxFactory.ExpressionStatement(
                                                                SyntaxFactory.InvocationExpression(
                                                                    SyntaxFactory.MemberAccessExpression(
                                                                        SyntaxKind.SimpleMemberAccessExpression,
                                                                        SyntaxFactory.ParseTypeName("global::System.Console"),
                                                                        SyntaxFactory.IdentifierName(nameof(Console.WriteLine))),
                                                                    SyntaxFactory.ArgumentList(
                                                                        SyntaxFactory.SingletonSeparatedList(
                                                                            SyntaxFactory.Argument(
                                                                                SyntaxFactory.InterpolatedStringExpression(
                                                                                    SyntaxFactory.Token(SyntaxKind.InterpolatedStringStartToken),
                                                                                    SyntaxFactory.List(
                                                                                        new InterpolatedStringContentSyntax[]
                                                                                        {
                                                                                            InterpolatedStringText("Intercepted!! ("),
                                                                                            SyntaxFactory.Interpolation(SyntaxFactory.IdentifierName("value")),
                                                                                            InterpolatedStringText(")"),
                                                                                        }))))))))))
                                                .AddAttributeLists(
                                                    attributeList
                                                        .Select(static attribute =>
                                                            SyntaxFactory.AttributeList(
                                                                SyntaxFactory.SingletonSeparatedList(attribute)))
                                                        .ToArray())))
                            }))
                    .WithLeadingTrivia(
                        SyntaxFactory.Comment("// <auto-generated/>"),
                        SyntaxFactory.Trivia(
                            SyntaxFactory.NullableDirectiveTrivia(
                                SyntaxFactory.Token(SyntaxKind.EnableKeyword),
                                true)));

                context.AddSource(
                    "Interceptor.cs",
                    compilationUnit.NormalizeWhitespace().ToFullString());
            });
    }

    private static InterpolatedStringTextSyntax InterpolatedStringText(
        string text)
    {
        return
            SyntaxFactory.InterpolatedStringText(
                SyntaxFactory.Token(
                    default,
                    SyntaxKind.InterpolatedStringTextToken,
                    text,
                    text,
                    default));
    }

    private static string GetInterceptorFilePath(
        SyntaxTree tree,
        Compilation compilation)
    {
        return compilation.Options.SourceReferenceResolver?.NormalizePath(tree.FilePath, baseFilePath: null) ?? tree.FilePath;
    }
}
