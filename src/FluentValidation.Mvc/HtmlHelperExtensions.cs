namespace FluentValidation.Mvc {
	using System.Text;
	using System.Web.Mvc;
	using System.Linq;

	public static class HtmlHelperExtensions {
		public static string RuleFor<T>(this HtmlHelper html) {
			return html.RuleFor<T>(null);
		}
		public static string RuleFor<T>(this HtmlHelper html, string prefix) {
			var validator = ValidatorOptions.CurrentValidatorFactory().GetValidator<T>();

			var validations = validator == null ? null : validator.CreateDescriptor().GetMembersWithValidators();

			if (validations == null || validations.Count == 0) { return "{}"; }

			var context = new HtmlRuleContext {
				RulesBuilder = new StringBuilder(),
				MessageBuilder = new StringBuilder(),
				Prefix = prefix,
			};

			foreach (var kvp in validations) {
				context.Key = kvp.Key;
				context.Validators = kvp.ToList();
				RuleFor(context);
				MessagesFor(context);
				context.RulesBuilder.Append(',');
			}

			if (context.RulesBuilder.Length > 1) {
				context.RulesBuilder.Insert(0, "rules:{");
				context.RulesBuilder.Remove(context.RulesBuilder.Length - 1, 1);
				context.RulesBuilder.Append('}');
			}
			if (context.MessageBuilder.Length > 1) {
				context.MessageBuilder.Insert(0, ", messages:{");
				context.MessageBuilder.Remove(context.MessageBuilder.Length - 1, 1);
				context.MessageBuilder.Append('}');
			}
			return string.Concat('{', context.RulesBuilder.ToString(), context.MessageBuilder.ToString(), '}');
		}


		private static void RuleFor(HtmlRuleContext context) {
			var sb = context.RulesBuilder;
			var startPosition = sb.Length;
			foreach (var validator in context.Validators) {
				if (!(validator is IDoJavascript)) { continue; }
				foreach (var property in ((IDoJavascript)validator).ToJson()) {
					sb.AppendFormat("{0}:{1}", property.Key, property.Value);
					sb.Append(",");
				}
			}
			if (sb.Length > 0) {
				sb.Remove(sb.Length - 1, 1);
				sb.Insert(startPosition, string.Concat(SafeKey(context), ":{"));
				sb.Append('}');
			}
		}
		private static void MessagesFor(HtmlRuleContext context) {
			foreach(var validator in context.Validators.Where(x => x is IDoJavascript).Where(x => x.UsingDefaultMesasge == false)) {
				context.MessageBuilder.AppendFormat("{0}: '{1}',", SafeKey(context), Escape(validator.ErrorMessageTemplate));
			}

//			if (string.IsNullOrEmpty(context.Data.Message)) { return; }
//			context.MessageBuilder.AppendFormat("{0}: '{1}',", SafeKey(context), Escape(context.Data.Message));
		}


		private static string Escape(string @string) {
			return string.IsNullOrEmpty(@string) ? string.Empty : @string.Replace("'", "\\'");
		}
		private static string SafeKey(HtmlRuleContext context) {
			return string.IsNullOrEmpty(context.Prefix) ? context.Key : string.Format("\"{0}.{1}\"", context.Prefix, context.Key);
		}        
	}
}