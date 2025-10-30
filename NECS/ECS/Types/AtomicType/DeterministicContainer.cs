
using NECS.ECS.ECSCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NECS.ECS.Types.AtomicType
{
    [System.Serializable]
    [TypeUid(106)]
    public class DeterministicContainer : BaseCustomType
    {
        public long _salt = 0;

        public DeterministicContainer()
        {
            
        }

        public DeterministicContainer(long salt)
        {
            _salt = salt;
        }

        /// <summary>
        /// Создает экземпляр процессора с указанной солью.
        /// </summary>
        /// <param name="salt">Значение long, используемое для инициализации генератора 
        /// случайных чисел.</param>
        public DeterministicContainer SetSalt(long salt)
        {
            _salt = salt;
            return this;
        }

        /// <summary>
        /// Вспомогательный метод для получения int "seed" из long "salt".
        /// GetHashCode() для long хорошо смешивает биты.
        /// </summary>
        private int GetSeed() => _salt.GetHashCode();

        // --- Функция 1: Детерминированная фильтрация ---

        /// <summary>
        /// Детерминированно фильтрует коллекцию на основе соли.
        /// Элементы будут либо включены, либо исключены с предсказуемой вероятностью.
        /// </summary>
        /// <typeparam name="T">Тип элементов в коллекции.</typeparam>
        /// <param name="collection">Исходная коллекция.</param>
        /// <returns>Новый массив, содержащий отфильтрованные элементы.</returns>
        public T[] DeterministicFilter<T>(IEnumerable<T> collection, int seedInject = 0)
        {
            // Создаем новый экземпляр Random с тем же сидом,
            // чтобы эта операция всегда начиналась одинаково.
            var rand = new Random(GetSeed() + seedInject);
            var filteredList = new List<T>();

            foreach (var item in collection)
            {
                // Пример детерминированной логики:
                // "Подбрасываем монетку" для каждого элемента.
                // Так как 'rand' инициализирован нашей солью, "монетка"
                // будет падать одинаково для одних и тех же элементов
                // в том же порядке при каждом вызове.
                if (rand.NextDouble() >= 0.5) // 50% шанс остаться
                {
                    filteredList.Add(item);
                }
            }

            return filteredList.ToArray();
        }

        // --- Функция 2: Детерминированная перестановка (тасование) ---

        /// <summary>
        /// Детерминированно "перемешивает" элементы коллекции в новом порядке
        /// на основе соли, используя алгоритм тасования Фишера-Йейтса.
        /// </summary>
        /// <typeparam name="T">Тип элементов в коллекции.</typeparam>
        /// <param name="collection">Исходная коллекция.</param>
        /// <returns>Новый массив с элементами в "случайном", но
        /// детерминированном порядке.</returns>
        public T[] DeterministicShuffle<T>(IEnumerable<T> collection, int seedInject = 0)
        {
            // Снова создаем Random с тем же сидом.
            var rand = new Random(GetSeed() + seedInject);

            // Нам нужен материализованный список/массив для тасования
            var array = collection.ToArray();
            int n = array.Length;

            // Алгоритм тасования Фишера-Йейтса
            // Идем с конца массива к началу
            for (int i = n - 1; i > 0; i--)
            {
                // Выбираем случайный индекс j от 0 до i (включительно)
                int j = rand.Next(i + 1);

                // Меняем элементы i и j местами
                T temp = array[i];
                array[i] = array[j];
                array[j] = temp;
            }

            return array;
        }

        // --- Функция 3: Обработка с детерминированным long ---

        /// <summary>
        /// Выполняет действие (Action) для каждого элемента коллекции, передавая
        /// в него сам элемент и детерминированное "случайное" long значение,
        /// сгенерированное на основе соли.
        /// </summary>
        /// <typeparam name="T">Тип элементов в коллекции.</typeparam>
        /// <param name="collection">Исходная коллекция.</param>
        /// <param name="action">Лямбда-функция или метод, принимающий 
        /// (T item, long deterministicValue).</param>
        public void ProcessWithDeterministicLong<T>(IEnumerable<T> collection, Action<T, long> action, int seedInject = 0)
        {
            // И снова создаем Random с тем же сидом.
            var rand = new Random(GetSeed() + seedInject);

            // Буфер для 8 байт (размер long)
            byte[] buffer = new byte[8];

            foreach (var item in collection)
            {
                // Генерируем 8 "случайных", но детерминированных байт
                rand.NextBytes(buffer);

                // Преобразуем байты в long
                long deterministicValue = BitConverter.ToInt64(buffer, 0);

                // Вызываем предоставленное действие
                action(item, deterministicValue);
            }
        }

        public T[] DeterministicSelect<T>(IEnumerable<T> collection, int count, int seedInject = 0)
        {
            if (collection == null || count <= 0)
            {
                return new T[0];
            }

            T[] shuffledArray = DeterministicShuffle(collection, seedInject);

            if (count >= shuffledArray.Length)
            {
                return shuffledArray;
            }

            return shuffledArray.Take(count).ToArray();
        }
    }
}