using System;
using System.ComponentModel;
using System.Linq.Expressions;

namespace Blacker.MangaScraper.ViewModel
{

    /// <summary>
    /// Base ViewModel class
    /// </summary>
    abstract class BaseViewModel : INotifyPropertyChanged
    {
        #region Implementation of INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged<T>(Expression<Func<T>> property)
        {
            var lambda = (LambdaExpression)property;
            MemberExpression memberExpression;

            var body = lambda.Body as UnaryExpression;
            if (body != null)
            {
                var unaryExpression = body;
                memberExpression = (MemberExpression)unaryExpression.Operand;
            }
            else
            {
                memberExpression = (MemberExpression)lambda.Body;
            }

            OnPropertyChanged(memberExpression.Member.Name);
        }

        private void OnPropertyChanged(string propertyName)
        {
            var e = new PropertyChangedEventArgs(propertyName);

            PropertyChangedEventHandler changed = PropertyChanged;

            if (changed != null)
            {
                changed(this, e);
            }
        }

        #endregion  // Implementation of INotifyPropertyChanged
    }
}
